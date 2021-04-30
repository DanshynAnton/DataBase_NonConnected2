using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace DatBase_Conected
{
    public partial class DBForm : Form
    {
        SqlConnection con;
        SqlCommand cmd;

        VehicleData vehDataWindow;
        RepairData repDataWindow;
        PathData pathDataWindow;

        /// <summary>
        /// Структура для получения введённых пользователем данных 
        /// для вставки и изменения в таблице VEHICLE
        /// </summary>
        public VehDataStruct veh = new VehDataStruct();

        /// <summary>
        /// Структура для получения введённых пользователем данных 
        /// для вставки и изменения в таблице REPAIRS
        /// </summary>

        public RepDataStruct rep = new RepDataStruct();

        /// <summary>
        /// Структура для получения введённых пользователем данных 
        /// для вставки и изменения в таблице PATHS
        /// </summary>
        public PathDataStruct myPath = new PathDataStruct();

        /// <summary>
        /// Словарь для хранения точек маршрута
        /// </summary>
        public Dictionary<int, string> dictPoints = new Dictionary<int, string>();

        public DBForm()
        {
            string DBSource = @"DESKTOP-07VPUSC\MSSQLSERVER01";
            string DBName = "AutoDB";

            InitializeComponent();
            //Создание соединения
            con = new SqlConnection();
            con.ConnectionString =  "Data Source = " + DBSource + "; \n" +
                                    "integrated security = true; \n" +
                                    "initial catalog = " + DBName + ";\n" +
                                    "connect timeout = 3;\n";
            try
            {
                //ОТкрываем соединение для проверки работоспособности
                con.Open();
                //MessageBox.Show("Connection is successfully opened!");
                this.Text = DBName;
            }
            catch (Exception)
            {
                MessageBox.Show("Connection is not opened. \nConncetion String:\n" + con.ConnectionString);
            }
            finally
            {
                //Закрываем соединение
                con.Close();
            }

            //Выделение сортировки по-умолчанию (по ID)
            rbSortVehID.Checked = true;
            rbSortRepID.Checked = true;
            rbSortPathID.Checked = true;

            myPath.Clear();
        }

        /// <summary>
        /// Получение ID точки маршрута по её имени
        /// </summary>
        /// <param name="pointName">имя точки маршрута</param>
        private int GetPointId(string pointName)
        {
            return dictPoints.FirstOrDefault(el => el.Value == pointName).Key;
        }

        /// <summary>
        /// Считывание данных из таблцы и занесение в dgv
        /// </summary>
        /// <param name="dgv">DataGridView</param>
        private void ReadTable(DataGridView dgv, string select)
        {
            //Получение данных в DataSet и ввод в DGV
            DataSet dSet = new DataSet();
            GetDataFromBD(select, out dSet);
            dgv.DataSource = dSet.Tables[0];
        }

        /// <summary>
        /// Получение данных из БД при помощи адаптеров
        /// </summary>
        /// <param name="ds">Виртуальное хранилище данных</param>
        private void GetDataFromBD(string command, out DataSet ds)
        {
            //Виртуальное хранилище
            ds = new DataSet();
            try
            {
                //Открываем соединение
                con.Open();
                //Адаптер
                SqlDataAdapter da = new SqlDataAdapter(command, con);
                //Отдаём данные из таблицы в DataSet
                da.Fill(ds);
                
            }
            catch (Exception) { }
            finally
            {
                //Разъединяем соединение
                con.Close();
            }
        }

        /// <summary>
        /// Выполнение вставки в БД
        /// </summary>
        /// <param name="insert">SQL-запрос на вставку</param>
        /// <returns>кол-во вставленных строк</returns>
        private int InsertToDB(string insert)
        {
            int result = 0;
            try
            {
                con.Open();
                //вставить данные
                cmd = new SqlCommand(insert, con);
                SqlDataAdapter da = new SqlDataAdapter();
                da.InsertCommand = cmd;
                result = da.InsertCommand.ExecuteNonQuery();
            }
            catch (Exception) { }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Выполнение редактирования в БД
        /// </summary>
        /// <param name="update">SQL-запрос на редактирование</param>
        /// <returns>кол-во вставленных строк</returns>
        private int UpdateInDB(string update)
        {
            int result = 0;
            try
            {
                con.Open();
                //изменить данные данные
                cmd = new SqlCommand(update, con);
                SqlDataAdapter da = new SqlDataAdapter();
                da.UpdateCommand = cmd;
                result = da.UpdateCommand.ExecuteNonQuery();
            }
            catch (Exception) { }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Выполнение удаления из БД
        /// </summary>
        /// <param name="delete">SQL-запрос на удаление</param>
        /// <returns>кол-во удалённых строк</returns>
        private int DeleteFromDB(string delete)
        {
            int result = 0;
            try
            {
                con.Open();
                //удалить данные
                cmd = new SqlCommand(delete, con);
                SqlDataAdapter da = new SqlDataAdapter();
                da.DeleteCommand = cmd;
                result = da.DeleteCommand.ExecuteNonQuery();
            }
            catch (Exception) { }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Закрытие формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DBForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (con.State == ConnectionState.Open)
            {
                //Отключаем соединение от базы данных, если оно не было закрыто
                con.Close();
            }
        }

        /**************************************************/
        /***************РАБОТА С ТРАНСПОРТОМ***************/
        /**************************************************/

        /// <summary>
        /// Вывод таблицы VEHICLE с фильтрами
        /// </summary>
        private void PrintVehicle()
        {
            //Получение данных из таблицы
            string select = @"SELECT v.veh_id as 'id',
                                     v.plate,
	                                 v.mileage,
	                                 v.full_cost,
	                                 p.path_id,
	                                 p.seating_position,
	                                 p.pessangers_capacity,
	                                 f.tlength as 'length',
	                                 f.twidth as 'width',
	                                 f.theight as 'height',
	                                 f.traffic,
	                                 f.load_capacity
                                FROM VEHICLE v
                                LEFT OUTER JOIN PASSANGER_TRANSPORT p
                                  ON v.veh_id = p.pt_id
                                LEFT OUTER JOIN FREIGHT_TRANSPORT f
                                  ON v.veh_id = f.ft_id " +
                                GetVehFilter() +
                                GetVehSort();
            ReadTable(dgvVehicle, select);
            //Редактирование вида табицы
            dgvVehicle.Columns[0].Width = 30;
            dgvVehicle.Columns[1].Width = 70;
            DataGridViewCellStyle dcs = new DataGridViewCellStyle();
            dcs.Font = new Font("Consolas", 9);
            dgvVehicle.Columns[1].DefaultCellStyle.ApplyStyle(dcs);
            dgvVehicle.Columns[2].Width = 60;
            dgvVehicle.Columns[3].Width = 80;

            dgvVehicle.Columns[4].Width = 50;
            dgvVehicle.Columns[5].Width = 33;
            dgvVehicle.Columns[6].Width = 33;
            dgvVehicle.Columns[7].Width = 45;
            dgvVehicle.Columns[8].Width = 45;
            dgvVehicle.Columns[9].Width = 45;
            dgvVehicle.Columns[10].Width = 60;
            dgvVehicle.Columns[11].Width = 45;
        }

        /// <summary>
        /// Формирование фильтра поиска транспорта на оснорве выбраных фильтров
        /// </summary>
        /// <returns>Выражение для с WHERE</returns>
        private string GetVehFilter()
        {
            string filter = "";
            int filterCount = 0;
            //Добавление фильтра номерного знака
            if (cbVehFilter.Checked)
            {
                filter += "(plate like '%" + tbVehFilter.Text + "%') ";
                filterCount++;
            }
            //Добавление фильтра минимального пробега
            if (cbMinMile.Checked)
            {
                if (filterCount > 0) { filter += " and "; }
                filter += "(mileage > " + nudMinMile.Value.ToString() + ") "; ;
                filterCount++;
            }
            //Добавление фильтра максимального пробега
            if (cbMaxMile.Checked)
            {
                if (filterCount > 0) { filter += " and "; }
                filter += "(mileage < " + nudMaxMile.Value.ToString() + ") ";
                filterCount++;
            }
            //Добавление WHEREE если было хотя бы одно условие
            if (filterCount > 0) { filter = " WHERE " + filter; }
            return filter;
        }

        /// <summary>
        /// Получение ORDER BY для транспорта на основе выбранной сортировки
        /// </summary>
        /// <returns></returns>
        private string GetVehSort()
        {
            string orderBy = "";
            //Направление сортировки
            string orderDir = (cbSortVehDesc.Checked ? " DESC" : "");
            //Добаление сортировки для транспрота
            if (rbSortVehID.Checked)
            {
                //Сортировка по ID
                orderBy = "ORDER BY veh_id" + orderDir;
            }
            else if (rbSortVehPlate.Checked)
            {
                //Сортировка по номерному знаку
                orderBy = "ORDER BY plate" + orderDir;
            }
            else if (rbSortVehPrice.Checked)
            {
                //Сортировка по стоимости обслуживания
                orderBy = "ORDER BY full_cost" + orderDir;
            }
            return orderBy;
        }

        /// <summary>
        /// Сортировка транспорта
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortVehicle(object sender, EventArgs e)
        {
            PrintVehicle();
        }

        /// <summary>
        /// Обработчик события изменения полей фильтров отображения транспорта
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeFilter_Handler(object sender, EventArgs e)
        {
            PrintVehicle();
        }

        /// <summary>
        /// Поиск гаража, в котором чинится транспорт по номерному знаку транспорта
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bFindVehByPlate_Click(object sender, EventArgs e)
        {
            //Формировавние запроса
            string select = "SELECT grg_id FROM GARAGES " +
                            "WHERE vehicle_id IN " +
                            "(SELECT veh_id FROM VEHICLE " +
                            "WHERE plate = '"+ tbVehFindPlate.Text + "')";

            //Получение данных в DataSet
            DataSet dSet = new DataSet();
            GetDataFromBD(select, out dSet);
           
            //Формирование ответа
            string resultMessage = "Транспорт с номером: " + tbVehFindPlate.Text + "\n";
            if (dSet.Tables[0].Rows.Count != 0)
            {
                //Найден гараж
                resultMessage += "\nНаходится в гараже с ID: " + dSet.Tables[0].Rows[0][0].ToString();
            }
            else
            {
                //Гараж не найден
                resultMessage += "\nНе найден ни в одном гараже.";
            }
            MessageBox.Show(resultMessage, "Поиск гаража");
        }

        /// <summary>
        /// Ввести данные в VEHICLE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bVehInsert_Click(object sender, EventArgs e)
        {
            try
            {
                //Очищение структуры данных
                veh.Clear();
                vehDataWindow = new VehicleData(this);
                vehDataWindow.Text = "Ввести новые данные";
                vehDataWindow.ShowDialog();

                //после закрытия окна, если данные были сохранены корректно
                if (veh.correct)
                {
                    try
                    {
                        string insert = "INSERT INTO VEHICLE (veh_id, box_id, plate, mileage, last_month_cost, full_cost, start_date_of_use) " +
                                        "VALUES (" + veh.veh_id + ", " + veh.box_id + ", '" + veh.plate + "', " + veh.mileage + ", " +
                                        Utilities.ReplaceComaToDot(veh.last_month_cost) + ", " + Utilities.ReplaceComaToDot(veh.full_cost) + ", " + veh.start_date_of_use + ")";
                        InsertToDB(insert);
                        PrintVehicle();
                    }
                    catch (Exception) { };
                }
            }
            catch (Exception) { };
        }

        /// <summary>
        /// Редактировать данные  в VEHICLE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bVehUpdate_Click(object sender, EventArgs e)
        {
            /**/
            try
            {
                veh.veh_id = tbIUDVehID.Text;

                //считать данные из VEHICLE и ввести в структуру данных
                string select = "SELECT veh_id, box_id, plate, mileage, last_month_cost, full_cost, start_date_of_use " +
                       "  FROM VEHICLE WHERE veh_id = " + veh.veh_id;
                DataSet ds = new DataSet();
                GetDataFromBD(select, out ds);
                DataRow dr = ds.Tables[0].Rows[0];
                //Внесение начальных данных
                veh.veh_id = dr[0].ToString();
                veh.box_id = dr[1].ToString();
                veh.plate = dr[2].ToString();
                veh.mileage = Utilities.StringOrNull(dr[3].ToString());
                veh.last_month_cost = Utilities.StringOrNull(dr[4].ToString());
                veh.full_cost = Utilities.StringOrNull(dr[5].ToString());
                veh.start_date_of_use = Utilities.StringOrNull(dr[6].ToString());

                //Создаём форму
                vehDataWindow = new VehicleData(this);
                vehDataWindow.Text = "Обновить данные по ID";
                //Передать данные на форму
                vehDataWindow.tbVehIUD_veh_id.Text = veh.veh_id;
                vehDataWindow.tbVehIUD_veh_id.Enabled = false;
                vehDataWindow.ShowDialog();

                //после закрытия окна, если данные были сохранены корректно
                if (veh.correct)
                {
                    string update = "UPDATE VEHICLE SET " +
                                    "box_id = " + veh.box_id + ", " +
                                    "plate = '" + veh.plate + "', " +
                                    "mileage = " + veh.mileage + ", " +
                                    "last_month_cost = " + Utilities.ReplaceComaToDot(veh.last_month_cost) + ", " +
                                    "full_cost = " + Utilities.ReplaceComaToDot(veh.full_cost) + ", " +
                                    "start_date_of_use =  " + veh.start_date_of_use + "\n" +
                                    "WHERE veh_id = " + veh.veh_id;
                    UpdateInDB(update);
                    PrintVehicle();
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Удалить данные из VEHICLE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bVehDelete_Click(object sender, EventArgs e)
        {

            try
            {
                veh.veh_id = tbIUDVehID.Text;
                //удалить данные из VEHICLE
                if (MessageBox.Show("Вы дествительно хотите удалить элемент с ID = " + veh.veh_id + "?", "Подтверждение удаления", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    //формирование запроса удаления
                    string delete = "DELETE FROM VEHICLE WHERE veh_id = " + veh.veh_id;
                    DeleteFromDB(delete);
                    PrintVehicle();
                }
            }
            catch (Exception) { };
        }

        /// <summary>
        /// Отключение кнопок удаления и редактирование маршрута, если не введён ID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbIUDPathID_TextChanged(object sender, EventArgs e)
        {
            if (tbIUDPathID.Text == "")
            {
                bPathUpdate.Enabled = false;
                bPathDelete.Enabled = false;
            }
            else
            {
                bPathUpdate.Enabled = true;
                bPathDelete.Enabled = true;
            }
        }

        /**************************************************/
        /*****************РАБОТА С РЕМОНТОМ****************/
        /**************************************************/

        /// <summary>
        /// Вывод таблицы REPAIR_INFO с сортировкой
        /// </summary>
        private void PrintRepair()
        {
            string select = "SELECT * " +
                "  FROM REPAIR_INFO " + GetRepSort();
            cmd = new SqlCommand(select, con);
            ReadTable(dgvRepairInfo, select);

            dgvRepairInfo.Columns[0].Width = 60;
            dgvRepairInfo.Columns[1].Width = 70;
            dgvRepairInfo.Columns[2].Width = 70;
        }

        /// <summary>
        /// Получение ORDER BY для ремонтных работ на сонове выбранной сортировки
        /// </summary>
        /// <returns></returns>
        private string GetRepSort()
        {
            string orderBy = "";
            //Направление сортировки
            string orderDir = (cbSortRepDesc.Checked ? " DESC" : "");
            if (rbSortRepID.Checked)
            {
                //Сортировка по ID
                orderBy = "ORDER BY repair_id" + orderDir;
            }
            else if (rbSortRepCost.Checked)
            {
                //Сортировка по полной стоимости
                orderBy = "ORDER BY full_cost" + orderDir;
            }
            return orderBy;
        }

        /// <summary>
        /// Сортировка таблицы с информацией о ремонте
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortRepair(object sender, EventArgs e)
        {
            PrintRepair();
        }

        /// <summary>
        /// Получение информации о ремонте по бригаде или транспорту (вычисления)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetSpecialRepair(object sender, EventArgs e)
        {
            //Колонка, в которой содержится ID
            int columnGroup = -1;
            string calcTypeMsg = "";
            string countID = tbRepairExID.Text;

            if (sender.Equals(bRepairCrew))
            {//Вычисление суммы работ на бригаду
                columnGroup = 1;
                calcTypeMsg = "на бригаду";
            }
            else if (sender.Equals(bRepairVehicle))
            {//Вычисление суммы работ на транспорта
                columnGroup = 2;
                calcTypeMsg = "по транспорту";
            }
            if (columnGroup != -1)
            {
                //корректный поиск по ID
                //Переменные для хренения подсчёта
                double repair_cost = 0;
                double part_cost = 0;
                double full_cost = 0;
                //Перебор всех строк
                int rowCount = dgvRepairInfo.RowCount;
                for (int i = rowCount - 1; i >= 0; i--)
                {
                    if (dgvRepairInfo.Rows[i].Cells[columnGroup].Value.ToString() == countID)
                    {//Нашли совпадения в строке
                        repair_cost += Convert.ToDouble(dgvRepairInfo.Rows[i].Cells[3].Value.ToString());
                        part_cost += Convert.ToDouble(dgvRepairInfo.Rows[i].Cells[4].Value.ToString());
                        full_cost += Convert.ToDouble(dgvRepairInfo.Rows[i].Cells[5].Value.ToString());
                    }
                }
                //Вывод сообщения с результатом
                MessageBox.Show("Сумма ремонта " + calcTypeMsg + " с ID: " + countID +
                                "\nсумма чистого ремонта: " + repair_cost.ToString() +
                                "\nсумма деталей ремонта: " + part_cost.ToString() +
                                "\nполная сумма ремонта: " + full_cost.ToString(), "Сумма ремонта " + calcTypeMsg);

            }
        }

        /// <summary>
        /// Поиск деталей, используемых при ремонте
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bFindPartsByRepID_Click(object sender, EventArgs e)
        {
            try
            {
                Convert.ToInt32(tbRepFindID.Text.ToString());
                //Формировавние запроса
                string select = "SELECT * FROM SPARE_PARTS WHERE repair_id =" + tbRepFindID.Text.ToString();

                //Получение данных в DataSet
                DataSet dSet = new DataSet();
                GetDataFromBD(select, out dSet);

                //Формирование ответа
                string resultMessage = "Детали для ремонта с ID = " + tbRepFindID.Text.ToString() + ":\n";
                string parts = "";
                if (dSet.Tables[0].Rows.Count != 0)
                {
                    foreach (DataRow el in dSet.Tables[0].Rows)
                    {
                        parts += "\n" + el[0].ToString();
                    }
                }
                else
                {
                    parts = "\nинформация не найдена";
                }
                resultMessage += parts;
                MessageBox.Show(resultMessage, "Поиск деталей");
            }
            catch (Exception)
            {
                //Введены некорретные данные
                MessageBox.Show("ОШИБКА!!!\nrepair_id должен быть целочисленным!\nВведённый repair_id: " + tbRepFindID.Text.ToString(), "Неверный идентификатор ремонта");
            }
        }

        /// <summary>
        /// Вставить данные в REPAIRS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bRepInsert_Click(object sender, EventArgs e)
        {
            try
            {
                //Очищение структуры данных
                rep.Clear();
                repDataWindow = new RepairData(this);
                repDataWindow.Text = "Ввести новые данные";
                repDataWindow.ShowDialog();

                //после закрытия окна, если данные были сохранены корректно
                if (rep.correct)
                {
                    try
                    {
                        string insert = "INSERT INTO REPAIRS (rep_id, crew_id, vehicle_id, cost) " +
                                        "VALUES (" + rep.repair_id + ", " + rep.crew_id + ", '" + rep.vehicle_id + "', " + Utilities.ReplaceComaToDot(rep.repair_cost) + ")";
                        InsertToDB(insert);
                        PrintVehicle();
                    }
                    catch (Exception) { };
                }
            }
            catch (Exception) { };
        }

        /// <summary>
        /// Изменить данные в REPAIRS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bRepUpdate_Click(object sender, EventArgs e)
        {
            
            try
            {
                rep.repair_id = tbIUDRepID.Text;

                //считать данные из VEHICLE и ввести в структуру данных
                string select = "SELECT rep_id, crew_id, vehicle_id, cost " +
                                "  FROM REPAIRS WHERE rep_id = " + rep.repair_id;
                DataSet ds = new DataSet();
                GetDataFromBD(select, out ds);
                DataRow dr = ds.Tables[0].Rows[0];
                //Внесение начальных данных
                //Внесение начальных данных
                //Данные найдены
                    rep.repair_id = dr[0].ToString();
                    rep.crew_id = Utilities.StringOrNull(dr[1].ToString());
                    rep.vehicle_id = Utilities.StringOrNull(dr[2].ToString());
                    rep.repair_cost = Utilities.StringOrNull(dr[3].ToString());

                //Создаём форму
                repDataWindow = new RepairData(this);
                repDataWindow.Text = "Обновить данные по ID";
                //Передать данные на форму
                repDataWindow.tbRepUID_repair_id.Text = rep.repair_id;
                repDataWindow.tbRepUID_repair_id.Enabled = false;
                repDataWindow.ShowDialog();

                //после закрытия окна, если данные были сохранены корректно
                if (rep.correct)
                {
                    string update = "UPDATE REPAIRS SET " +
                                    "crew_id = " + rep.crew_id + ", " +
                                    "vehicle_id = " + rep.vehicle_id + ", " +
                                    "cost = " + Utilities.ReplaceComaToDot(rep.repair_cost) + " " +
                                    "WHERE rep_id = " + rep.repair_id;
                    UpdateInDB(update);
                    PrintRepair();
                }
            }
            catch (Exception) { };
        }

        /// <summary>
        /// Удалить данные из REPAIRS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bRepDelete_Click(object sender, EventArgs e)
        {
            try
            {

                rep.repair_id = tbIUDRepID.Text;
                if (MessageBox.Show("Вы дествительно хотите удалить элемент с ID = " + rep.repair_id + "?", "Подтверждение удаления", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    //удалить данные из REPAIR
                    string delete = "DELETE FROM REPAIRS WHERE rep_id = " + rep.repair_id;
                    DeleteFromDB(delete);
                    PrintRepair();
                }
            }
            catch (Exception) { };
        }

        /// <summary>
        /// Отключение кнопок удаления и редактирование ремонта, если не введён ID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbIUDRepID_TextChanged(object sender, EventArgs e)
        {
            if (tbIUDRepID.Text == "")
            {
                bRepUpdate.Enabled = false;
                bRepDelete.Enabled = false;
            }
            else
            {
                bRepUpdate.Enabled = true;
                bRepDelete.Enabled = true;
            }
        }

        /**************************************************/
        /***************РАБОТА С ПЕРЕВОЗКАМИ***************/
        /**************************************************/

        /// <summary>
        /// Вывод ифнормации о маршрутах в таблицу
        /// </summary>
        private void PrintPath()
        {
            string select = "SELECT * " +
                "  FROM PATHS " + GetPathSort();
            ReadTable(dgvPaths, select);
            GetPointList();
        }

        /// <summary>
        /// Получение сортировки ORDER BY для вывода PATHS
        /// </summary>
        /// <returns></returns>
        private string GetPathSort()
        {
            string orderBy = "";
            //Направление сортировки
            string orderDir = (cbSortPathsDesc.Checked ? " DESC" : "");
            if (rbSortPathID.Checked)
            {
                //Сортировка по ID
                orderBy = "ORDER BY path_id" + orderDir;
            }
            else if (rbSortPathPass.Checked)
            {
                //Сортировка по полной стоимости
                orderBy = "ORDER BY passangers" + orderDir;
            }
            return orderBy;
        }

        /// <summary>
        /// Сортировка таблицы с маршрутами
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SortPaths(object sender, EventArgs e)
        {
            PrintPath();
        }

        /// <summary>
        /// Получение WHERE для поиска путей по точкам
        /// </summary>
        /// <returns></returns>
        private string GetPointForWhere()
        {
            string points = "";
            //Значение последнего id. Нужно для того чтобы "Перекрыть" последнюю запятую
            string lastID = "";
            //Поиск ключа по значению
            foreach (var p in lbPoints.SelectedItems)
            {
                lastID = GetPointId(p as string).ToString();
                points += lastID + ", ";
            }
            points += lastID;
            return points;
        }

        /// <summary>
        /// Получение списка пунктов маршрута
        /// </summary>
        private void GetPointList()
        {
            //Получение данных из таблицы
            string select = "SELECT * FROM POINTS";
            DataSet ds = new DataSet();
            GetDataFromBD(select, out ds);

            //Очищение словваря и списка
            dictPoints.Clear();
            lbPoints.Items.Clear();

            //Занесение данных в словарь и список
            foreach (DataRow el in ds.Tables[0].Rows)
            {
                dictPoints.Add(Convert.ToInt32(el[0]), el[1].ToString());
                lbPoints.Items.Add(el[1].ToString());
            }
        }

        /// <summary>
        /// Изменение выделения поля списка пунктов маршрута
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbPoints_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbPoints.SelectedItems.Count == 0) { bFindPathByPoints.Enabled = false; }
            else { bFindPathByPoints.Enabled = true; }
        }

        /// <summary>
        /// Поиск маршрута, проходящего через точки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bFindPathByPoints_Click(object sender, EventArgs e)
        {
            string select = "SELECT DISTINCT(path_id) FROM POINT_PATHS " +
                            "WHERE point_id IN (" + GetPointForWhere() + ")";
            //Получение данных в DataSet
            DataSet dSet = new DataSet();
            GetDataFromBD(select, out dSet);

            //Занесение данных строку с ответом
            string resultMsg = "Через выбранные точки проходят маршруты:\n";
            if (dSet.Tables[0].Rows.Count != 0)
            {
                foreach (DataRow el in dSet.Tables[0].Rows)
                {
                    resultMsg += "\n" + el[0].ToString();
                }
            }
            else
            {
                resultMsg = "Не найдено маршрутов через данные точки";
            }

            MessageBox.Show(resultMsg, "Поиск маршрутов");
        }

        /// <summary>
        /// Очищение поля выбора пунктов маршрута
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bClearPoints_Click(object sender, EventArgs e)
        {
            lbPoints.ClearSelected();
        }

        /// <summary>
        /// Вставить данные в PATHS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bPathInsert_Click(object sender, EventArgs e)
        {
            try
            {
                //Очищение структуры данных
                myPath.Clear();
                pathDataWindow = new PathData(this);
                pathDataWindow.Text = "Ввести новые данные";
                pathDataWindow.ShowDialog();

                //после закрытия окна, если данные были сохранены корректно
                if (myPath.correct)
                {
                    string insert = "INSERT INTO PATHS (path_id, passangers) " +
                                    "VALUES ('" + myPath.path_id + "', " + myPath.passangers + ")";
                    InsertToDB(insert);
                    //Вставка точек маршурта в POINT_PATHS
                    foreach (var point in myPath.pointList)
                    {
                        string insertPoint = "INSERT INTO POINT_PATHS (path_id, point_id) " +
                                       "VALUES ('" + myPath.path_id + "', " + GetPointId(point).ToString() + ")";

                        InsertToDB(insert);
                    }
                    PrintPath();
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Изменить данные в PATHS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bPathUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                myPath.path_id = tbIUDPathID.Text;

                //считать данные из VEHICLE и ввести в структуру данных
                string select = "SELECT path_id, passangers " +
                                "  FROM PATHS WHERE path_id = '" + myPath.path_id + "'";
                DataSet ds = new DataSet();
                GetDataFromBD(select, out ds);
                DataRow dr = ds.Tables[0].Rows[0];
                //Внесение начальных данных

                    myPath.path_id = dr[0].ToString();
                    myPath.passangers = dr[1].ToString();

                //Считываем данные о точках на маршруте
                string selectPoints = "SELECT pname FROM POINTS WHERE point_id IN (SELECT DISTINCT(point_id) FROM POINT_PATHS " +
                                        "WHERE path_id ='" + myPath.path_id + "')";
                ds = new DataSet();
                GetDataFromBD(select, out ds);
               int  pathCount = 0;
                foreach (DataRow drp in ds.Tables[0].Rows)
                {
                    //Занесение данных строку с ответом
                    myPath.pointList[pathCount] = dr[0].ToString();
                    pathCount++;
                }

                //Создаём форму
                pathDataWindow = new PathData(this);
                pathDataWindow.Text = "Обновить данные по ID";
                //Передать данные на форму
                pathDataWindow.tbPathIUD_path_id.Text = myPath.path_id;
                pathDataWindow.tbPathIUD_path_id.Enabled = false;
                pathDataWindow.ShowDialog();

                //после закрытия окна, если данные были сохранены корректно
                if (myPath.correct)
                {
                    string update = "UPDATE PATHS SET " +
                                    "passangers = " + myPath.passangers + " " +
                                    "WHERE path_id = '" + myPath.path_id + "'";
                    UpdateInDB(update);

                    //Удаление точек маршрута ииз POINT_PATHS
                    string delete = "DELETE FROM POINT_PATHS WHERE path_id = '" + myPath.path_id + "'";
                    DeleteFromDB(delete);
                    //Вставка новых точек маршурта в POINT_PATHS
                    foreach (var point in myPath.pointList)
                    {
                        string insertPoint = "INSERT INTO POINT_PATHS (path_id, point_id) " +
                                       "VALUES ('" + myPath.path_id + "', " + GetPointId(point).ToString() + ")";
                        InsertToDB(insertPoint);
                    }
                    PrintPath();
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Удалить данные из PATHS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bPathDelete_Click(object sender, EventArgs e)
        {
            try
            {
                myPath.path_id = tbIUDPathID.Text;
                if (MessageBox.Show("Вы дествительно хотите удалить элемент с ID = '" + myPath.path_id +"'?", "Подтверждение удаления", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    //удалить данные из REPAIR
                    string delete = "DELETE FROM PATHS WHERE path_id = '" + myPath.path_id + "'";
                    DeleteFromDB(delete);
                    PrintPath();
                }
            }
            catch (Exception) { };
        }

        /// <summary>
        /// Отключение кнопок удаления и редактирование транспорта, если не введён ID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbIUDVehID_TextChanged(object sender, EventArgs e)
        {
            if (tbIUDVehID.Text == "")
            {
                bVehUpdate.Enabled = false;
                bVehDelete.Enabled = false;
            }
            else
            {
                bVehUpdate.Enabled = true;
                bVehDelete.Enabled = true;
            }
        }
    }
}

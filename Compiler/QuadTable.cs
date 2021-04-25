using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Compiler
{
    class QuadTable
    {
        public DataTable quad;

        DataColumn column;
        DataRow row;
        public int labelCounter = 1;
        public bool labelNext = false;
        public Stack<string> labelStack;
        public QuadTable() 
        {
            quad = new DataTable("QuadTable");
            labelStack = new Stack<string>();

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Label";
            quad.Columns.Add(column);

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Operator";
            quad.Columns.Add(column);

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Operand_1";
            quad.Columns.Add(column);

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Operand_2";
            quad.Columns.Add(column);

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Operand_3";
            quad.Columns.Add(column);

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Comment";
            quad.Columns.Add(column);
        }

        public void AddRow(string label, string op, string oper1, string oper2, string oper3, string comment) 
        {
            row = quad.NewRow();
            if (labelNext)
            {
                if (!label.Contains("StaticInit"))
                {
                    row["Label"] = labelStack.Pop();
                }
                else
                {
                    BackPatch(labelStack.Pop(), label);
                    row["Label"] = label;
                }
                labelNext = false;
            }
            else 
            {
                row["Label"] = label;
            }
            row["Operator"] = op;
            row["Operand_1"] = oper1;
            row["Operand_2"] = oper2;
            row["Operand_3"] = oper3;
            row["Comment"] = comment;

            quad.Rows.Add(row);
        }

        /*
            BackPatch and replace labels as needed
         */
        public void BackPatch(string oldLabel, string newLabel)
        {
            foreach (DataRow row in quad.Rows) 
            {
                for (int i = 0; i < 5; i++) 
                {
                    if (row[i].ToString() == oldLabel)
                    {
                        row[i] = newLabel;
                    }
                }
            }
        }
        /*
            Print out the entire table formatted for visibility
         */
        public void ToString() 
        {
            Console.WriteLine("Quad Table:\n");
            Console.WriteLine("{0,12}{1,12}{2,12}{3,12}{4,12}{5,50}", "LABEL", "Operator", "Operand_1", "Operand_2", "Operand_3", "Comment");
            foreach (DataRow row in quad.Rows)
            {
                Console.WriteLine("{0,12}{1,12}{2,12}{3,12}{4,12}{5,50}", row[0], row[1], row[2], row[3], row[4], row[5]);
            }
        }
        /*
            Get the top row of the table
         */
        internal DataRow GetTopRow()
        {
            return quad.Rows[0];
        }
        /*
            Get the top row of the table
         */
        internal DataRow DeleteTopRow()
        {
            quad.Rows[0].Delete();
            DataRow x = quad.Rows[0];
            return x;
        }
        /*
            Get the bottom row of the table
         */
        internal DataRow GetBotRow()
        {
            return quad.Rows[quad.Rows.Count - 1];
        }

        public string[] RemoveBotRow() 
        {
            string[] returnStrArr = new string[6];
            returnStrArr[0] = (string)quad.Rows[quad.Rows.Count - 1][0];
            returnStrArr[1] = (string)quad.Rows[quad.Rows.Count - 1][1];
            returnStrArr[2] = (string)quad.Rows[quad.Rows.Count - 1][2];
            returnStrArr[3] = (string)quad.Rows[quad.Rows.Count - 1][3];
            returnStrArr[4] = (string)quad.Rows[quad.Rows.Count - 1][4];
            returnStrArr[5] = (string)quad.Rows[quad.Rows.Count - 1][5];

            quad.Rows.RemoveAt(quad.Rows.Count - 1);

            return returnStrArr;
        }

        public void addQuad(QuadTable sQuad) 
        {
            int counter = 0;
            foreach (DataRow row in sQuad.quad.Rows)
            {
                string[] returnStrArr = new string[6];

                returnStrArr[0] = (string)sQuad.quad.Rows[counter][0];
                returnStrArr[1] = (string)sQuad.quad.Rows[counter][1];
                returnStrArr[2] = (string)sQuad.quad.Rows[counter][2];
                returnStrArr[3] = (string)sQuad.quad.Rows[counter][3];
                returnStrArr[4] = (string)sQuad.quad.Rows[counter][4];
                returnStrArr[5] = (string)sQuad.quad.Rows[counter][5];

                AddRow(returnStrArr[0], returnStrArr[1],returnStrArr[2],returnStrArr[3],returnStrArr[4],returnStrArr[5]);
                counter++;
            }
        }
    }
}

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  Document,
  HeadingLevel,
  Packer,
  Paragraph,
  Table,
  TableCell,
  TableRow,
  TextRun,
  WidthType
} from "docx";
import XLSX from "xlsx";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const projectRoot = path.resolve(__dirname, "..", "..");
const outputDir = path.join(projectRoot, "docs", "sample-reports");

fs.mkdirSync(outputDir, { recursive: true });

const salesRows = [
  ["Дата", "Товар", "Количество", "Сумма"],
  ["01.04.2026", "Ноутбук Lenovo ThinkBook", "4", "1680000"],
  ["03.04.2026", "Монитор Samsung 27\"", "6", "1140000"],
  ["05.04.2026", "Смартфон Galaxy A55", "10", "1950000"],
  ["08.04.2026", "Наушники Sony WH-CH520", "14", "490000"],
  ["12.04.2026", "Клавиатура Logitech K380", "9", "351000"],
  ["18.04.2026", "Мышь Logitech MX Anywhere", "11", "517000"],
  ["22.04.2026", "Планшет Xiaomi Pad 6", "5", "925000"],
  ["27.04.2026", "SSD Kingston 1TB", "13", "702000"]
];

const financialRows = [
  ["Период", "Доход", "Расходы", "Прибыль"],
  ["Январь 2026", "4250000", "3010000", "1240000"],
  ["Февраль 2026", "4480000", "3090000", "1390000"],
  ["Март 2026", "4720000", "3200000", "1520000"],
  ["Апрель 2026", "4950000", "3315000", "1635000"],
  ["Май 2026", "5210000", "3480000", "1730000"]
];

const educationRows = [
  ["Ученик", "Предмет", "Оценка", "Средний балл"],
  ["Алина Сарсенова", "Математика", "5", "4.8"],
  ["Алина Сарсенова", "Информатика", "5", "4.8"],
  ["Даниил Орлов", "Математика", "4", "4.2"],
  ["Даниил Орлов", "История", "4", "4.2"],
  ["Мария Жунусова", "Физика", "5", "4.9"],
  ["Мария Жунусова", "Химия", "5", "4.9"],
  ["Тимур Ахметов", "Английский язык", "3", "3.6"],
  ["Тимур Ахметов", "Биология", "4", "3.6"],
  ["София Ким", "Литература", "5", "4.7"],
  ["София Ким", "География", "4", "4.7"]
];

async function createDocxReport(fileName, title, subtitle, note, rows) {
  const doc = new Document({
    sections: [
      {
        children: [
          new Paragraph({
            text: title,
            heading: HeadingLevel.TITLE
          }),
          new Paragraph({
            children: [new TextRun({ text: subtitle, bold: true })]
          }),
          new Paragraph({
            text: note
          }),
          new Paragraph({ text: "" }),
          new Table({
            width: { size: 100, type: WidthType.PERCENTAGE },
            rows: rows.map((row, index) => new TableRow({
              children: row.map((value) => new TableCell({
                children: [
                  new Paragraph({
                    children: [
                      new TextRun({
                        text: value,
                        bold: index === 0
                      })
                    ]
                  })
                ]
              }))
            }))
          })
        ]
      }
    ]
  });

  const buffer = await Packer.toBuffer(doc);
  fs.writeFileSync(path.join(outputDir, fileName), buffer);
}

function createXlsReport(fileName, sheetName, rows) {
  const workbook = XLSX.utils.book_new();
  const worksheet = XLSX.utils.aoa_to_sheet(rows);
  worksheet["!cols"] = rows[0].map((column) => ({
    wch: Math.max(column.length + 2, 18)
  }));

  XLSX.utils.book_append_sheet(workbook, worksheet, sheetName);
  XLSX.writeFile(workbook, path.join(outputDir, fileName), { bookType: "biff8" });
}

await createDocxReport(
  "Типовой_отчет_по_продажам.docx",
  "Типовой отчет по продажам",
  "Период: апрель 2026",
  "Документ предназначен для демонстрации импорта продаж в BizAnalytics. Таблица содержит дату, товар, количество и сумму продаж.",
  salesRows
);

await createDocxReport(
  "Типовой_финансовый_отчет.docx",
  "Типовой финансовый отчет",
  "Период: январь - май 2026",
  "Документ предназначен для демонстрации финансовой аналитики в BizAnalytics. Таблица содержит период, доход, расходы и прибыль.",
  financialRows
);

createXlsReport(
  "Типовой_отчет_по_успеваемости.xls",
  "Успеваемость",
  educationRows
);

console.log(`Sample reports created in: ${outputDir}`);

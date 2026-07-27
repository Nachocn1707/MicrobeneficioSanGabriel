using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MicrobeneficioSanGabriel.Models;

namespace MicrobeneficioSanGabriel.Services
{
    public class FacturaDocument : IDocument
    {
        public Factura Model { get; }
        public string? LogoPath { get; }

        public FacturaDocument(Factura model, string? logoPath = null)
        {
            Model = model;
            LogoPath = logoPath;
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = $"Factura #{Model.Id}"
        };

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor("#1f2937").FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                if (!string.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath))
                {
                    column.Item().AlignCenter().Width(120).Image(LogoPath);
                    column.Item().Height(10);
                }

                column.Item().AlignCenter().Text("Microbeneficio San Gabriel")
                    .FontSize(24).SemiBold().FontColor("#111827");

                column.Item().Height(4);

                column.Item().AlignCenter().Text("Factura electrónica")
                    .FontSize(12).FontColor("#6b7280");

                column.Item().Height(15);
                column.Item().LineHorizontal(1).LineColor("#e5e7eb");
                column.Item().Height(20);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Border(1).BorderColor("#e5e7eb").Padding(24).Column(card =>
                {
                    card.Item().Text("Información de factura").FontSize(16).Bold().FontColor("#111827");
                    card.Item().Height(10);

                    card.Item().Row(row =>
                    {
                        row.AutoItem().Text(t =>
                        {
                            t.Span("Factura #: ").Bold();
                            t.Span(Model.Id.ToString());
                        });
                    });

                    card.Item().Height(5);
                    card.Item().Row(row =>
                    {
                        row.AutoItem().Text(t =>
                        {
                            t.Span("Fecha: ").Bold();
                            t.Span(Model.FechaFactura.ToString("dd/MM/yyyy"));
                        });
                    });

                    card.Item().Height(5);
                    card.Item().Row(row =>
                    {
                        row.AutoItem().Text(t =>
                        {
                            t.Span("Estado: ").Bold();
                            t.Span(Model.EstadoPago ?? "Pendiente");
                        });
                    });

                    card.Item().Height(15);
                    card.Item().LineHorizontal(1).LineColor("#e5e7eb");
                    card.Item().Height(15);

                    card.Item().Text("Cliente").FontSize(16).Bold().FontColor("#111827");
                    card.Item().Height(8);
                    card.Item().Text(Model.Pedido?.ClienteNombre ?? "Sin cliente registrado").FontSize(12);

                    card.Item().Height(15);

                    card.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(45);
                            columns.RelativeColumn(20);
                            columns.RelativeColumn(35);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#1f2937").Border(1).BorderColor("#1f2937").Padding(10).AlignCenter().Text("Producto").Bold().FontColor(Colors.White);
                            header.Cell().Background("#1f2937").Border(1).BorderColor("#1f2937").Padding(10).AlignCenter().Text("Cantidad (kg)").Bold().FontColor(Colors.White);
                            header.Cell().Background("#1f2937").Border(1).BorderColor("#1f2937").Padding(10).AlignCenter().Text("Subtotal").Bold().FontColor(Colors.White);
                        });

                        var productoNombre = Model.Pedido?.Producto?.Nombre ?? "Sin producto";
                        var cantidadStr = Model.Pedido != null ? $"{Model.Pedido.Cantidad} kg" : "0 kg";
                        var subtotalStr = $"₡ {Model.Subtotal:N0}";

                        table.Cell().Border(1).BorderColor("#d1d5db").Padding(10).AlignCenter().Text(productoNombre);
                        table.Cell().Border(1).BorderColor("#d1d5db").Padding(10).AlignCenter().Text(cantidadStr);
                        table.Cell().Border(1).BorderColor("#d1d5db").Padding(10).AlignCenter().Text(subtotalStr);
                    });

                    card.Item().Height(25);

                    card.Item().AlignRight().Width(240).Border(2).BorderColor("#16a34a").Background("#f0fdf4").Padding(12).Column(totalBox =>
                    {
                        totalBox.Item().Row(row =>
                        {
                            row.RelativeItem().Text("IVA:").FontSize(14).Bold().FontColor("#16a34a");
                            row.RelativeItem().AlignRight().Text($"₡ {Model.IVA:N0}").FontSize(14).Bold().FontColor("#16a34a");
                        });

                        totalBox.Item().Height(6);

                        totalBox.Item().Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL:").FontSize(14).Bold().FontColor("#16a34a");
                            row.RelativeItem().AlignRight().Text($"₡ {Model.Total:N0}").FontSize(14).Bold().FontColor("#16a34a");
                        });
                    });
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor("#e5e7eb");
                column.Item().Height(10);
                column.Item().AlignCenter().Text("Gracias por su compra - Microbeneficio San Gabriel")
                    .FontSize(10).FontColor("#6b7280");
            });
        }
    }
}

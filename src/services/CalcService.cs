csharp
using System;
using System.Collections.Generic;

namespace InvestCalc.Services
{
    public class CalcService
    {
        public class InvestmentScenario
        {
            public string Name { get; set; } = "Магазин одежды StylePoint";
            public string Type { get; set; } = "local"; // "local" или "cloud"
            
            // Финансовые показатели
            public decimal Capex { get; set; } = 3_200_000;           // CAPEX для local
            public decimal AnnualOpex { get; set; } = 1_450_000;      // OPEX в год для local
            public decimal AnnualSubscription { get; set; } = 1_512_000; // Подписка в год для cloud
            public decimal Implementation { get; set; } = 850_000;    // Внедрение для cloud
            public decimal Training { get; set; } = 400_000;          // Обучение персонала
            
            // Период анализа
            public int Period { get; set; } = 5;                      // Лет
            
            // Ожидаемые выгоды
            public decimal AnnualSavings { get; set; } = 2_800_000;   // Экономия в год
            public decimal AnnualRevenueGrowth { get; set; } = 1_500_000; // Рост доходов в год
        }
        
        public class CalculationResult
        {
            public decimal Tco { get; set; }                // Полная стоимость владения
            public decimal Roi { get; set; }                // ROI в процентах
            public decimal PaybackPeriod { get; set; }      // Срок окупаемости в годах
            public decimal TotalBenefits { get; set; }      // Общая выгода
            public Dictionary<string, decimal> Sensitivity { get; set; } = new(); // Анализ чувствительности
        }
        
        public decimal CalculateTCO(InvestmentScenario scenario)
        {
            if (scenario.Type == "local")
            {
                // Локальная модель: TCO_local = CAPEX + OPEX * period + training
                return scenario.Capex + 
                       (scenario.AnnualOpex * scenario.Period) + 
                       scenario.Training;
            }
            else // cloud
            {
                // Облачная модель: TCO_cloud = implementation + subscription * period + training
                return scenario.Implementation + 
                       (scenario.AnnualSubscription * scenario.Period) + 
                       scenario.Training;
            }
        }
        
        public (decimal Benefits, decimal Roi) CalculateROI(InvestmentScenario scenario, decimal tco)
        {
            // Benefits = (savings + revenue_growth) * period
            decimal benefits = (scenario.AnnualSavings + scenario.AnnualRevenueGrowth) * scenario.Period;
            
            // ROI = ((Benefits - TCO) / TCO) * 100
            decimal roi = tco == 0 ? 0 : ((benefits - tco) / tco) * 100;
            
            return (benefits, Math.Round(roi, 2));
        }
        
        public decimal CalculatePaybackPeriod(InvestmentScenario scenario, decimal tco)
        {
            // PP = TCO / (savings + revenue_growth)
            decimal annualNetBenefit = scenario.AnnualSavings + scenario.AnnualRevenueGrowth;
            
            if (annualNetBenefit == 0) return 0;
            
            return Math.Round(tco / annualNetBenefit, 2);
        }
        
        public CalculationResult CalculateAll(InvestmentScenario scenario)
        {
            var result = new CalculationResult();
// 1. Расчет TCO
            result.Tco = CalculateTCO(scenario);
            
            // 2. Расчет ROI
            var (benefits, roi) = CalculateROI(scenario, result.Tco);
            result.TotalBenefits = benefits;
            result.Roi = roi;
            
            // 3. Расчет срока окупаемости
            result.PaybackPeriod = CalculatePaybackPeriod(scenario, result.Tco);
            
            // 4. Анализ чувствительности ±20%
            result.Sensitivity = CalculateSensitivity(scenario);
            
            return result;
        }
        
        private Dictionary<string, decimal> CalculateSensitivity(InvestmentScenario scenario)
        {
            var sensitivity = new Dictionary<string, decimal>();
            decimal variation = 0.2m; // ±20%
            
            // Базовый расчет
            var baseResult = CalculateAll(scenario);
            
            // 1. Анализ чувствительности по экономии (savings)
            var scenarioLowSavings = CloneScenario(scenario);
            scenarioLowSavings.AnnualSavings *= (1 - variation);
            var resultLowSavings = CalculateAll(scenarioLowSavings);
            sensitivity["roi_low_savings"] = resultLowSavings.Roi;
            
            var scenarioHighSavings = CloneScenario(scenario);
            scenarioHighSavings.AnnualSavings *= (1 + variation);
            var resultHighSavings = CalculateAll(scenarioHighSavings);
            sensitivity["roi_high_savings"] = resultHighSavings.Roi;
            
            // 2. Анализ чувствительности по росту доходов (revenue_growth)
            var scenarioLowRevenue = CloneScenario(scenario);
            scenarioLowRevenue.AnnualRevenueGrowth *= (1 - variation);
            var resultLowRevenue = CalculateAll(scenarioLowRevenue);
            sensitivity["roi_low_revenue"] = resultLowRevenue.Roi;
            
            var scenarioHighRevenue = CloneScenario(scenario);
            scenarioHighRevenue.AnnualRevenueGrowth *= (1 + variation);
            var resultHighRevenue = CalculateAll(scenarioHighRevenue);
            sensitivity["roi_high_revenue"] = resultHighRevenue.Roi;
            
            // 3. Анализ чувствительности по OPEX/Subscription
            if (scenario.Type == "local")
            {
                var scenarioHighOpex = CloneScenario(scenario);
                scenarioHighOpex.AnnualOpex *= (1 + variation);
                var resultHighOpex = CalculateAll(scenarioHighOpex);
                sensitivity["roi_high_opex"] = resultHighOpex.Roi;
            }
            else // cloud
            {
                var scenarioHighSub = CloneScenario(scenario);
                scenarioHighSub.AnnualSubscription *= (1 + variation);
                var resultHighSub = CalculateAll(scenarioHighSub);
                sensitivity["roi_high_subscription"] = resultHighSub.Roi;
            }
            
            // 4. Анализ чувствительности по CAPEX/Implementation
            if (scenario.Type == "local")
            {
                var scenarioHighCapex = CloneScenario(scenario);
                scenarioHighCapex.Capex *= (1 + variation);
                var resultHighCapex = CalculateAll(scenarioHighCapex);
                sensitivity["roi_high_capex"] = resultHighCapex.Roi;
            }
            
            return sensitivity;
        }
        
        private InvestmentScenario CloneScenario(InvestmentScenario original)
        {
            return new InvestmentScenario
            {
                Name = original.Name,
                Type = original.Type,
                Capex = original.Capex,
                AnnualOpex = original.AnnualOpex,
                AnnualSubscription = original.AnnualSubscription,
                Implementation = original.Implementation,
                Training = original.Training,
                Period = original.Period,
                AnnualSavings = original.AnnualSavings,
                AnnualRevenueGrowth = original.AnnualRevenueGrowth
            };
        }
    }
}

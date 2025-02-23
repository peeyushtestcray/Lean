/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using MathNet.Numerics.RootFinding;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Logging;
using QuantConnect.Python;
using QuantConnect.Util;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Implied Volatility indicator that calculate the IV of an option using Black-Scholes Model
    /// </summary>
    public class ImpliedVolatility : OptionIndicatorBase
    {
        private BaseDataConsolidator _consolidator;
        private RateOfChange _roc;
        private decimal _impliedVolatility;
        private Func<decimal, decimal, decimal> SmoothingFunction;

        /// <summary>
        /// Gets the historical volatility of the underlying
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> HistoricalVolatility { get; }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : base(name, option, riskFreeRateModel, dividendYieldModel, optionModel, period)
        {
            _roc = new(1);
            HistoricalVolatility = IndicatorExtensions.Times(
                IndicatorExtensions.Of(
                    new StandardDeviation(period),
                    _roc
                ),
                Convert.ToDecimal(Math.Sqrt(252))
            );
            
            _consolidator = new(TimeSpan.FromDays(1));
            _consolidator.DataConsolidated += (_, bar) => {
                _roc.Update(bar.EndTime, bar.Price);
            };
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this($"IV({option.Value},{riskFreeRateModel},{dividendYieldModel},{optionModel},{period})", option, riskFreeRateModel, dividendYieldModel, optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(string name, Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this(name, option, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel), 
                DividendYieldModelPythonWrapper.FromPyObject(dividendYieldModel), optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this($"IV({option.Value},{riskFreeRateModel},{dividendYieldModel},{optionModel},{period})", option, riskFreeRateModel, dividendYieldModel, optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this(name, option, riskFreeRateModel, new ConstantDividendYieldModel(dividendYield), optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this($"IV({option.Value},{riskFreeRateModel},{dividendYield},{optionModel},{period})", option, riskFreeRateModel, dividendYield, optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(string name, Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this(name, option, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel), 
                new ConstantDividendYieldModel(dividendYield), optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this($"IV({option.Value},{riskFreeRateModel},{dividendYield},{optionModel},{period})", option, riskFreeRateModel, dividendYield, optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(string name, Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this(name, option, new ConstantRiskFreeRateInterestRateModel(riskFreeRate), new ConstantDividendYieldModel(dividendYield), optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this($"IV({option.Value},{riskFreeRate},{dividendYield},{optionModel},{period})", option, riskFreeRate, dividendYield, optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(string name, Symbol option, Symbol mirrorOption, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this(name, option, riskFreeRateModel, dividendYieldModel, optionModel, period)
        {
            SetMirrorOptionContract(mirrorOption);

            // Default smoothing function will be assuming Law of One Price hold,
            // so both call and put will have the same IV
            // and using on OTM/ATM options to calculate the IV
            // by assuming extra volatility coming from extrinsic value
            SmoothingFunction = (impliedVol, mirrorImpliedVol) =>
            {
                if (Strike > UnderlyingPrice && Right == OptionRight.Put)
                {
                    return mirrorImpliedVol;
                }
                else if (Strike < UnderlyingPrice && Right == OptionRight.Call)
                {
                    return mirrorImpliedVol;
                }
                return impliedVol;
            };
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(Symbol option, Symbol mirrorOption, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this($"IV({option.Value},{riskFreeRateModel},{dividendYieldModel},{optionModel},{period})", option, mirrorOption, riskFreeRateModel, dividendYieldModel, optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(string name, Symbol option, Symbol mirrorOption, PyObject riskFreeRateModel, PyObject dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this(name, option, mirrorOption, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel), 
                DividendYieldModelPythonWrapper.FromPyObject(dividendYieldModel), optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(Symbol option, Symbol mirrorOption, PyObject riskFreeRateModel, PyObject dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this($"IV({option.Value},{mirrorOption.Value},{riskFreeRateModel},{dividendYieldModel},{optionModel},{period})", option, mirrorOption, riskFreeRateModel, dividendYieldModel, optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(string name, Symbol option, Symbol mirrorOption, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this(name, option, mirrorOption, riskFreeRateModel, new ConstantDividendYieldModel(dividendYield), optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(Symbol option, Symbol mirrorOption, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this($"IV({option.Value},{mirrorOption.Value},{riskFreeRateModel},{dividendYield},{optionModel},{period})", option, mirrorOption, riskFreeRateModel, dividendYield, optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(string name, Symbol option, Symbol mirrorOption, PyObject riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this(name, option, mirrorOption, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel), 
                new ConstantDividendYieldModel(dividendYield), optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(Symbol option, Symbol mirrorOption, PyObject riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this($"IV({option.Value},{mirrorOption.Value},{riskFreeRateModel},{dividendYield},{optionModel},{period})", option, mirrorOption, riskFreeRateModel, dividendYield, optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(string name, Symbol option, Symbol mirrorOption, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this(name, option, mirrorOption, new ConstantRiskFreeRateInterestRateModel(riskFreeRate), new ConstantDividendYieldModel(dividendYield), optionModel, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(Symbol option, Symbol mirrorOption, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252)
            : this($"IV({option.Value},{mirrorOption.Value},{riskFreeRate},{dividendYield},{optionModel},{period})", option, mirrorOption, riskFreeRate, dividendYield, optionModel, period)
        {
        }

        /// <summary>
        /// Set the smoothing function of IV, using both call and put IV value
        /// </summary>
        /// <param name="function">the smoothing function</param>
        public void SetSmoothingFunction(Func<decimal, decimal, decimal> function)
        {
            SmoothingFunction = function;
        }

        /// <summary>
        /// Set the smoothing function of IV, using both call and put IV value
        /// </summary>
        /// <param name="function">the smoothing function</param>
        public void SetSmoothingFunction(PyObject function)
        {
            SmoothingFunction = PythonUtil.ToFunc<decimal, decimal, decimal>(function);
        }

        private bool _isReady => Price.Current.Time == UnderlyingPrice.Current.Time && Price.IsReady && UnderlyingPrice.IsReady;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => UseMirrorContract ? _isReady && Price.Current.Time == OppositePrice.Current.Time && OppositePrice.IsReady : _isReady;

        /// <summary>
        /// Computes the next value
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            if (input.Symbol == _optionSymbol)
            {
                Price.Update(input.EndTime, input.Price);
            }
            else if (input.Symbol == _oppositeOptionSymbol)
            {
                OppositePrice.Update(input.EndTime, input.Price);
            }
            else if (input.Symbol == _underlyingSymbol)
            {
                _consolidator.Update(input);
                UnderlyingPrice.Update(input.EndTime, input.Price);
            }
            else
            {
                throw new ArgumentException("The given symbol was not target or reference symbol");
            }

            var time = Price.Current.Time;
            if (_isReady)
            {
                if (UseMirrorContract)
                {
                    if (time != OppositePrice.Current.Time)
                    {
                        return _impliedVolatility;
                    }
                }

                _impliedVolatility = CalculateIV(time);
            }

            return _impliedVolatility;
        }

        // Calculate the theoretical option price
        private decimal TheoreticalPrice(decimal volatility, decimal spotPrice, decimal strikePrice, decimal timeToExpiration, decimal riskFreeRate, 
            decimal dividendYield, OptionRight optionType, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
        {
            switch (optionModel)
            {
                // Binomial model also follows BSM process (log-normal)
                case OptionPricingModelType.BinomialCoxRossRubinstein:
                    return OptionGreekIndicatorsHelper.CRRTheoreticalPrice(volatility, spotPrice, strikePrice, timeToExpiration, riskFreeRate, dividendYield, optionType);
                case OptionPricingModelType.ForwardTree:
                    return OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(volatility, spotPrice, strikePrice, timeToExpiration, riskFreeRate, dividendYield, optionType);
                case OptionPricingModelType.BlackScholes:
                default:
                    return OptionGreekIndicatorsHelper.BlackTheoreticalPrice(volatility, spotPrice, strikePrice, timeToExpiration, riskFreeRate, dividendYield, optionType);
            }
        }

        /// <summary>
        /// Computes the IV of the option
        /// </summary>
        /// <param name="time">the datetime at this calculation</param>
        /// <returns>Smoothened IV of the option</returns>
        protected virtual decimal CalculateIV(DateTime time)
        {
            RiskFreeRate.Update(time, _riskFreeInterestRateModel.GetInterestRate(time));
            DividendYield.Update(time, _dividendYieldModel.GetDividendYield(time));

            var timeToExpiration = Convert.ToDecimal((Expiry - time).TotalDays) / 365m;

            var impliedVol = 0m;
            try
            {
                Func<double, double> f = (vol) => (double)(TheoreticalPrice(
                    Convert.ToDecimal(vol), UnderlyingPrice, Strike, timeToExpiration, RiskFreeRate, DividendYield, Right, _optionModel) - Price);
                impliedVol = Convert.ToDecimal(Brent.FindRoot(f, 1e-7d, 2.0d, 1e-4d, 100));
            }
            catch
            {
                Log.Error("ImpliedVolatility.CalculateIV(): Fail to converge, returning 0.");
            }

            if (UseMirrorContract)
            {
                var mirrorImpliedVol = 0m;
                try
                {
                    Func<double, double> f = (vol) => (double)(TheoreticalPrice(
                        Convert.ToDecimal(vol), UnderlyingPrice, Strike, timeToExpiration, RiskFreeRate, DividendYield, _oppositeOptionSymbol.ID.OptionRight, _optionModel) - OppositePrice);
                    mirrorImpliedVol = Convert.ToDecimal(Brent.FindRoot(f, 1e-7d, 2.0d, 1e-4d, 100));
                }
                catch
                {
                    Log.Error("ImpliedVolatility.CalculateIV(): Fail to converge, returning 0.");
                }

                return SmoothingFunction(impliedVol, mirrorImpliedVol);
            }

            return impliedVol;
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators
        /// </summary>
        public override void Reset()
        {
            _consolidator.Dispose();
            _consolidator = new(TimeSpan.FromDays(1));
            _consolidator.DataConsolidated += (_, bar) => {
                _roc.Update(bar.EndTime, bar.Price);
            };

            _roc.Reset();
            HistoricalVolatility.Reset();
            base.Reset();
        }
    }
}

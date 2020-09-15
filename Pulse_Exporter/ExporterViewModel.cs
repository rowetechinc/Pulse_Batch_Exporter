/*
 * Copyright 2011, Rowe Technology Inc. 
 * All rights reserved.
 * http://www.rowetechinc.com
 * https://github.com/rowetechinc
 * 
 * Redistribution and use in source and binary forms, with or without modification, are
 * permitted provided that the following conditions are met:
 * 
 *  1. Redistributions of source code must retain the above copyright notice, this list of
 *      conditions and the following disclaimer.
 *      
 *  2. Redistributions in binary form must reproduce the above copyright notice, this list
 *      of conditions and the following disclaimer in the documentation and/or other materials
 *      provided with the distribution.
 *      
 *  THIS SOFTWARE IS PROVIDED BY Rowe Technology Inc. ''AS IS'' AND ANY EXPRESS OR IMPLIED 
 *  WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 *  FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> OR
 *  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 *  CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 *  SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
 *  ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 *  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 *  ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *  
 * The views and conclusions contained in the software and documentation are those of the
 * authors and should not be interpreted as representing official policies, either expressed
 * or implied, of Rowe Technology Inc.
 * 
 * HISTORY
 * -----------------------------------------------------------------
 * Date            Initials    Version    Comments
 * -----------------------------------------------------------------
 * 07/23/2017      RC          1.0.0      Initial coding
 * 10/06/2017      RC          1.1.0      Added screening options.
 * 12/10/2018      RC          1.8.0      Fixed when to close files when selecting multiple files.
 * 07/31/2019      RC          1.13.1     Added option to select GPS or BT to remove the ship speed.
 * 11/29/2019      RC          1.13.3     Added Heading, Pitch and Roll offset.
 * 
 */

using Caliburn.Micro;
using ReactiveUI.Legacy;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;
using RTI;


namespace Pulse_Exporter
{
    /// <summary>
    /// Export the data to selected format.
    /// </summary>
    class ExporterViewModel : Caliburn.Micro.Screen
    {

        #region Variables

        /// <summary>
        ///  Setup logger
        /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Beam coordiante transform.
        /// </summary>
        private const string XFORM_BEAM = "BEAM";

        /// <summary>
        /// Instrument coordiante transform.
        /// </summary>
        private const string XFORM_INSTRUMENT = "Instrument";

        /// <summary>
        /// Earth coordiante transform.
        /// </summary>
        private const string XFORM_EARTH = "Earth";

        #region Previous Vel

        /// <summary>
        /// Previous Good Bottom Track East velocity.
        /// </summary>
        private float _prevBtEast = RTI.DataSet.Ensemble.BAD_VELOCITY;

        /// <summary>
        /// Previous Good Bottom Track North velocity.
        /// </summary>
        private float _prevBtNorth = RTI.DataSet.Ensemble.BAD_VELOCITY;

        /// <summary>
        /// Previous Good Bottom Track Vertical velocity.
        /// </summary>
        private float _prevBtVert = RTI.DataSet.Ensemble.BAD_VELOCITY;

        /// <summary>
        /// Previous Ship Speed X.  Used to remove ship speed from Velocity data.
        /// </summary>
        private float _prevShipSpeedX = RTI.DataSet.Ensemble.BAD_VELOCITY;

        /// <summary>
        /// Previous Ship Speed Y.  Used to remove ship speed from Velocity data.
        /// </summary>
        private float _prevShipSpeedY = RTI.DataSet.Ensemble.BAD_VELOCITY;

        /// <summary>
        /// Previous Ship Speed Z.  Used to remove ship speed from Velocity data.
        /// </summary>
        private float _prevShipSpeedZ = RTI.DataSet.Ensemble.BAD_VELOCITY;

        /// <summary>
        /// Previous Ship Speed Transverse.  Used to remove ship speed from Velocity data.
        /// </summary>
        private float _prevShipSpeedTransverse = RTI.DataSet.Ensemble.BAD_VELOCITY;

        /// <summary>
        /// Previous Ship Speed Longitundial.  Used to remove ship speed from Velocity data.
        /// </summary>
        private float _prevShipSpeedLongitudinal = RTI.DataSet.Ensemble.BAD_VELOCITY;

        /// <summary>
        /// Previous Ship Speed Normal.  Used to remove ship speed from Velocity data.
        /// </summary>
        private float _prevShipSpeedNormal = RTI.DataSet.Ensemble.BAD_VELOCITY;

        #endregion

        /// <summary>
        /// Store the export writers for each configuration. 
        /// </summary>
        private Dictionary<SubsystemConfiguration, ExportWriter> _exportWriters;

        #endregion

        #region Enum

        /// <summary>
        /// Export options.
        /// </summary>
        private enum Exporters
        {
            /// <summary>
            /// Export ensembles to CSV
            /// </summary>
            CSV,

            /// <summary>
            /// Export ensembles to matlab.
            /// </summary>
            Matlab,

            /// <summary>
            /// Export ensembles to PD0.
            /// </summary>
            PD0,

            /// <summary>
            /// Export ensembles to Waves Matlab file.
            /// </summary>
            Waves
        }

        #endregion

        #region Properties

        /// <summary>
        /// Export options.
        /// </summary>
        public string Version
        {
            get { return System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString(); ; }
        }

        /// <summary>
        /// Export options.
        /// </summary>
        private ExportOptions _Options;
        /// <summary>
        /// Export options.
        /// </summary>
        public ExportOptions Options
        {
            get { return _Options; }
            set
            {
                _Options = value;
                this.NotifyOfPropertyChange(() => this.Options);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// File path to store the results.
        /// </summary>
        private string _ResultsFilePath;
        /// <summary>
        /// File path to store the results.
        /// </summary>
        public string ResultsFilePath
        {
            get { return _ResultsFilePath; }
            set
            {
                _ResultsFilePath = PathAddBackslash(value);

                this.NotifyOfPropertyChange(() => this.ResultsFilePath);
            }
        }

        /// <summary>
        /// Flag to show loading symbol.
        /// </summary>
        private bool _IsExporting;
        /// <summary>
        /// Flag to show loading symbol.
        /// </summary>
        public bool IsExporting
        {
            get { return _IsExporting; }
            set
            {
                _IsExporting = value;
                this.NotifyOfPropertyChange(() => this.IsExporting);
            }
        }

        #region Toggle Exports

        /// <summary>
        /// Flag to if CSV export is selected.
        /// </summary>
        private bool _IsCsvSelected;
        /// <summary>
        /// Flag to if CSV export is selected.
        /// </summary>
        public bool IsCsvSelected
        {
            get { return _IsCsvSelected; }
            set
            {
                _IsCsvSelected = value;
                this.NotifyOfPropertyChange(() => this.IsCsvSelected);
            }
        }

        /// <summary>
        /// Flag to if Matlab export is selected.
        /// </summary>
        private bool _IsMatlabSelected;
        /// <summary>
        /// Flag to if Matlab export is selected.
        /// </summary>
        public bool IsMatlabSelected
        {
            get { return _IsMatlabSelected; }
            set
            {
                _IsMatlabSelected = value;
                this.NotifyOfPropertyChange(() => this.IsMatlabSelected);
            }
        }

        /// <summary>
        /// Flag to if Matlab Matrix export is selected.
        /// </summary>
        private bool _IsMatlabMatrixSelected;
        /// <summary>
        /// Flag to if Matlab Matrix export is selected.
        /// </summary>
        public bool IsMatlabMatrixSelected
        {
            get { return _IsMatlabMatrixSelected; }
            set
            {
                _IsMatlabMatrixSelected = value;
                this.NotifyOfPropertyChange(() => this.IsMatlabMatrixSelected);
            }
        }

        /// <summary>
        /// Flag to if PD0 export is selected.
        /// </summary>
        private bool _IsPd0Selected;
        /// <summary>
        /// Flag to if PD0 export is selected.
        /// </summary>
        public bool IsPd0Selected
        {
            get { return _IsPd0Selected; }
            set
            {
                _IsPd0Selected = value;
                this.NotifyOfPropertyChange(() => this.IsPd0Selected);
            }
        }

        /// <summary>
        /// Flag to if ENS export is selected.
        /// </summary>
        private bool _IsEnsSelected;
        /// <summary>
        /// Flag to if ENS export is selected.
        /// </summary>
        public bool IsEnsSelected
        {
            get { return _IsEnsSelected; }
            set
            {
                _IsEnsSelected = value;
                this.NotifyOfPropertyChange(() => this.IsEnsSelected);
            }
        }

        #endregion

        #region Bins

        /// <summary>
        /// Minimum Bin.
        /// </summary>
        private int _MinimumBin;
        /// <summary>
        /// Minimum Bin.
        /// </summary>
        public int MinimumBin
        {
            get { return _MinimumBin; }
            set
            {
                _MinimumBin = value;
                this.NotifyOfPropertyChange(() => this.MinimumBin);
            }
        }

        /// <summary>
        /// Maximum Bin.
        /// </summary>
        private int _MaximumBin;
        /// <summary>
        /// Maximum Bin.
        /// </summary>
        public int MaximumBin
        {
            get { return _MaximumBin; }
            set
            {
                _MaximumBin = value;
                this.NotifyOfPropertyChange(() => this.MaximumBin);
            }
        }

        #endregion

        #region Ensemble Numbers

        /// <summary>
        /// Minimum Ensemble Number.
        /// </summary>
        public uint MinEnsembleNumber
        {
            get { return _Options.MinEnsembleNumber; }
            set
            {
                _Options.MinEnsembleNumber = value;
                this.NotifyOfPropertyChange(() => this.MinEnsembleNumber);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Maximum Ensemble Number.
        /// </summary>
        public uint MaxEnsembleNumber
        {
            get { return _Options.MaxEnsembleNumber; }
            set
            {
                _Options.MaxEnsembleNumber = value;
                this.NotifyOfPropertyChange(() => this.MaxEnsembleNumber);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Minimum Ensemble Number Entry.
        /// </summary>
        private uint _MinEnsembleNumberEntry;
        /// <summary>
        /// Minimum Ensemble Number Entry.
        /// </summary>
        public uint MinEnsembleNumberEntry
        {
            get { return _MinEnsembleNumberEntry; }
            set
            {
                _MinEnsembleNumberEntry = value;
                this.NotifyOfPropertyChange(() => this.MinEnsembleNumberEntry);
            }
        }

        /// <summary>
        /// Maximum Ensemble Number Entry.
        /// </summary>
        private uint _MaxEnsembleNumberEntry;
        /// <summary>
        /// Maximum Ensemble Number Entry.
        /// </summary>
        public uint MaxEnsembleNumberEntry
        {
            get { return _MaxEnsembleNumberEntry; }
            set
            {
                _MaxEnsembleNumberEntry = value;
                this.NotifyOfPropertyChange(() => this.MaxEnsembleNumberEntry);
            }
        }

        #endregion

        #region Amplitude

        /// <summary>
        /// Amplitude enabled/disabled.
        /// </summary>
        public bool IsAmplitudeDataSetOn
        {
            get { return _Options.IsAmplitudeDataSetOn; }
            set
            {
                _Options.IsAmplitudeDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsAmplitudeDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Amplitude minimum bin.
        /// </summary>
        public int AmplitudeMinBin
        {
            get { return _Options.AmplitudeMinBin; }
            set
            {
                _Options.AmplitudeMinBin = value;
                this.NotifyOfPropertyChange(() => this.AmplitudeMinBin);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Amplitude maximum bin.
        /// </summary>
        public int AmplitudeMaxBin
        {
            get { return _Options.AmplitudeMaxBin; }
            set
            {
                _Options.AmplitudeMaxBin = value;
                this.NotifyOfPropertyChange(() => this.AmplitudeMaxBin);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Beam Velocity

        /// <summary>
        /// Beam Velocity enabled/disabled.
        /// </summary>
        public bool IsBeamVelocityDataSetOn
        {
            get { return _Options.IsBeamVelocityDataSetOn; }
            set
            {
                _Options.IsBeamVelocityDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsBeamVelocityDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Beam Velocity minimum bin.
        /// </summary>
        public int BeamMinBin
        {
            get { return _Options.BeamMinBin; }
            set
            {
                _Options.BeamMinBin = value;
                this.NotifyOfPropertyChange(() => this.BeamMinBin);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Beam Velocity maximum bin.
        /// </summary>
        public int BeamMaxBin
        {
            get { return _Options.BeamMaxBin; }
            set
            {
                _Options.BeamMaxBin = value;
                this.NotifyOfPropertyChange(() => this.BeamMaxBin);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Instrument Velocity

        /// <summary>
        /// Instrument Velocity enabled/disabled.
        /// </summary>
        public bool IsInstrumentVelocityDataSetOn
        {
            get { return _Options.IsInstrumentVelocityDataSetOn; }
            set
            {
                _Options.IsInstrumentVelocityDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsInstrumentVelocityDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Instrument Velocity minimum bin.
        /// </summary>
        public int InstrumentMinBin
        {
            get { return _Options.InstrumentMinBin; }
            set
            {
                _Options.InstrumentMinBin = value;
                this.NotifyOfPropertyChange(() => this.InstrumentMinBin);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Instrument Velocity maximum bin.
        /// </summary>
        public int InstrumentMaxBin
        {
            get { return _Options.InstrumentMaxBin; }
            set
            {
                _Options.InstrumentMaxBin = value;
                this.NotifyOfPropertyChange(() => this.InstrumentMaxBin);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Earth Velocity

        /// <summary>
        /// Earth Velocity enabled/disabled.
        /// </summary>
        public bool IsEarthVelocityDataSetOn
        {
            get { return _Options.IsEarthVelocityDataSetOn; }
            set
            {
                _Options.IsEarthVelocityDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsEarthVelocityDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Earth Velocity minimum bin.
        /// </summary>
        public int EarthMinBin
        {
            get { return _Options.EarthMinBin; }
            set
            {
                _Options.EarthMinBin = value;
                this.NotifyOfPropertyChange(() => this.EarthMinBin);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Earth Velocity maximum bin.
        /// </summary>
        public int EarthMaxBin
        {
            get { return _Options.EarthMaxBin; }
            set
            {
                _Options.EarthMaxBin = value;
                this.NotifyOfPropertyChange(() => this.EarthMaxBin);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Ship Velocity

        /// <summary>
        /// Earth Velocity enabled/disabled.
        /// </summary>
        public bool IsShipVelocityDataSetOn
        {
            get { return _Options.IsShipVelocityDataSetOn; }
            set
            {
                _Options.IsShipVelocityDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsShipVelocityDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Ship Velocity minimum bin.
        /// </summary>
        public int ShipMinBin
        {
            get { return _Options.ShipMinBin; }
            set
            {
                _Options.ShipMinBin = value;
                this.NotifyOfPropertyChange(() => this.ShipMinBin);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Ship Velocity maximum bin.
        /// </summary>
        public int ShipMaxBin
        {
            get { return _Options.ShipMaxBin; }
            set
            {
                _Options.ShipMaxBin = value;
                this.NotifyOfPropertyChange(() => this.ShipMaxBin);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Velocity Vector

        /// <summary>
        /// Velocity Vector enabled/disabled.
        /// </summary>
        public bool IsVelocityVectorDataSetOn
        {
            get { return _Options.IsVelocityVectorDataSetOn; }
            set
            {
                _Options.IsVelocityVectorDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsVelocityVectorDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Velocity Vector minimum bin.
        /// </summary>
        public int VelVectorMinBin
        {
            get { return _Options.VelVectorMinBin; }
            set
            {
                _Options.VelVectorMinBin = value;
                this.NotifyOfPropertyChange(() => this.VelVectorMinBin);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Velocity Vector maximum bin.
        /// </summary>
        public int VelVectorMaxBin
        {
            get { return _Options.VelVectorMaxBin; }
            set
            {
                _Options.VelVectorMaxBin = value;
                this.NotifyOfPropertyChange(() => this.VelVectorMaxBin);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Coordinate Transform

        /// <summary>
        /// Coordinate Transform.
        /// </summary>
        private string _CoordinateTransform;
        /// <summary>
        /// Coordinate Transform.
        /// </summary>
        public string CoordinateTransform
        {
            get { return _CoordinateTransform; }
            set
            {
                _CoordinateTransform = value;
                this.NotifyOfPropertyChange(() => this.CoordinateTransform);

                // Beam
                if (_CoordinateTransform == XFORM_BEAM)
                {
                    _Options.CoordinateTransform = PD0.CoordinateTransforms.Coord_Beam;
                }

                // Instrument
                if (_CoordinateTransform == XFORM_INSTRUMENT)
                {
                    _Options.CoordinateTransform = PD0.CoordinateTransforms.Coord_Instrument;
                }

                // Earth
                if (_CoordinateTransform == XFORM_EARTH)
                {
                    _Options.CoordinateTransform = PD0.CoordinateTransforms.Coord_Earth;
                }

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// List of all the coordinate Transforms to choose.
        /// </summary>
        public List<string> CoordinateTransformList { get; set; }

        #endregion

        #region Correlation

        /// <summary>
        /// Correlation enabled/disabled.
        /// </summary>
        public bool IsCorrelationDataSetOn
        {
            get { return _Options.IsCorrelationDataSetOn; }
            set
            {
                _Options.IsCorrelationDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsCorrelationDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Correlation minimum bin.
        /// </summary>
        public int CorrelationMinBin
        {
            get { return _Options.CorrelationMinBin; }
            set
            {
                _Options.CorrelationMinBin = value;
                this.NotifyOfPropertyChange(() => this.CorrelationMinBin);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Correlation maximum bin.
        /// </summary>
        public int CorrelationMaxBin
        {
            get { return _Options.CorrelationMaxBin; }
            set
            {
                _Options.CorrelationMaxBin = value;
                this.NotifyOfPropertyChange(() => this.CorrelationMaxBin);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Good Beam

        /// <summary>
        /// Good Beam enabled/disabled.
        /// </summary>
        public bool IsGoodBeamDataSetOn
        {
            get { return _Options.IsGoodBeamDataSetOn; }
            set
            {
                _Options.IsGoodBeamDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsGoodBeamDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Good Beam minimum bin.
        /// </summary>
        public int GoodBeamMinBin
        {
            get { return _Options.GoodBeamMinBin; }
            set
            {
                _Options.GoodBeamMinBin = value;
                this.NotifyOfPropertyChange(() => this.GoodBeamMinBin);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Good Beam maximum bin.
        /// </summary>
        public int GoodBeamMaxBin
        {
            get { return _Options.GoodBeamMaxBin; }
            set
            {
                _Options.GoodBeamMaxBin = value;
                this.NotifyOfPropertyChange(() => this.GoodBeamMaxBin);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Good Earth

        /// <summary>
        /// Good Earth enabled/disabled.
        /// </summary>
        public bool IsGoodEarthDataSetOn
        {
            get { return _Options.IsGoodEarthDataSetOn; }
            set
            {
                _Options.IsGoodEarthDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsGoodEarthDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Good Earth minimum bin.
        /// </summary>
        public int GoodEarthMinBin
        {
            get { return _Options.GoodEarthMinBin; }
            set
            {
                _Options.GoodEarthMinBin = value;
                this.NotifyOfPropertyChange(() => this.GoodEarthMinBin);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Good Earth maximum bin.
        /// </summary>
        public int GoodEarthMaxBin
        {
            get { return _Options.GoodEarthMaxBin; }
            set
            {
                _Options.GoodEarthMaxBin = value;
                this.NotifyOfPropertyChange(() => this.GoodEarthMaxBin);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Bottom Track

        /// <summary>
        /// Bottom Track enabled/disabled.
        /// </summary>
        public bool IsBottomTrackDataSetOn
        {
            get { return _Options.IsBottomTrackDataSetOn; }
            set
            {
                _Options.IsBottomTrackDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsBottomTrackDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Earth Water Mass

        /// <summary>
        /// Earth Water Mass enabled/disabled.
        /// </summary>
        public bool IsEarthWaterMassDataSetOn
        {
            get { return _Options.IsEarthWaterMassDataSetOn; }
            set
            {
                _Options.IsEarthWaterMassDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsEarthWaterMassDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Instrument Water Mass

        /// <summary>
        /// Instrument Water Mass enabled/disabled.
        /// </summary>
        public bool IsInstrumentWaterMassDataSetOn
        {
            get { return _Options.IsInstrumentWaterMassDataSetOn; }
            set
            {
                _Options.IsInstrumentWaterMassDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsInstrumentWaterMassDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Ship Water Mass

        /// <summary>
        /// Ship Water Mass enabled/disabled.
        /// </summary>
        public bool IsShipWaterMassDataSetOn
        {
            get { return _Options.IsShipWaterMassDataSetOn; }
            set
            {
                _Options.IsShipWaterMassDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsShipWaterMassDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Range Tracking

        /// <summary>
        /// Range Tracking enabled/disabled.
        /// </summary>
        public bool IsRangeTrackingDataSetOn
        {
            get { return _Options.IsRangeTrackingDataSetOn; }
            set
            {
                _Options.IsRangeTrackingDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsRangeTrackingDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Gage Height

        /// <summary>
        /// Gage Height enabled/disabled.
        /// </summary>
        public bool IsGageHeightDataSetOn
        {
            get { return _Options.IsGageHeightDataSetOn; }
            set
            {
                _Options.IsGageHeightDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsGageHeightDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region NMEA

        /// <summary>
        /// NMEA enabled/disabled.
        /// </summary>
        public bool IsNmeaDataSetOn
        {
            get { return _Options.IsNmeaDataSetOn; }
            set
            {
                _Options.IsNmeaDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsNmeaDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Profile Engineering

        /// <summary>
        /// Profile Engineering enabled/disabled.
        /// </summary>
        public bool IsProfileEngineeringDataSetOn
        {
            get { return _Options.IsProfileEngineeringDataSetOn; }
            set
            {
                _Options.IsProfileEngineeringDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsProfileEngineeringDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Bottom Track Engineering

        /// <summary>
        /// Bottom Track Engineering enabled/disabled.
        /// </summary>
        public bool IsBottomTrackEngineeringDataSetOn
        {
            get { return _Options.IsBottomTrackEngineeringDataSetOn; }
            set
            {
                _Options.IsBottomTrackEngineeringDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsBottomTrackEngineeringDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region System Setup

        /// <summary>
        /// System Setup enabled/disabled.
        /// </summary>
        public bool IsSystemSetupDataSetOn
        {
            get { return _Options.IsSystemSetupDataSetOn; }
            set
            {
                _Options.IsSystemSetupDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsSystemSetupDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region ADCP GPS Data

        /// <summary>
        /// ADCP GPS Data enabled/disabled.
        /// </summary>
        public bool IsAdcpGpsDataSetOn
        {
            get { return _Options.IsAdcpGpsDataSetOn; }
            set
            {
                _Options.IsAdcpGpsDataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsAdcpGpsDataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region GPS 1

        /// <summary>
        /// GPS 1 Data enabled/disabled.
        /// </summary>
        public bool IsGps1DataSetOn
        {
            get { return _Options.IsGps1DataSetOn; }
            set
            {
                _Options.IsGps1DataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsGps1DataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region GPS 2

        /// <summary>
        /// GPS 2 Data enabled/disabled.
        /// </summary>
        public bool IsGps2DataSetOn
        {
            get { return _Options.IsGps2DataSetOn; }
            set
            {
                _Options.IsGps2DataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsGps2DataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region NMEA 1

        /// <summary>
        /// NMEA 1 Data enabled/disabled.
        /// </summary>
        public bool IsNmea1DataSetOn
        {
            get { return _Options.IsNmea1DataSetOn; }
            set
            {
                _Options.IsNmea1DataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsNmea1DataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region NMEA 2

        /// <summary>
        /// NMEA 2 Data enabled/disabled.
        /// </summary>
        public bool IsNmea2DataSetOn
        {
            get { return _Options.IsNmea2DataSetOn; }
            set
            {
                _Options.IsNmea2DataSetOn = value;
                this.NotifyOfPropertyChange(() => this.IsNmea2DataSetOn);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Screen

        /// <summary>
        /// Retransform the data.
        /// </summary>
        public bool IsRetransformData
        {
            get { return _Options.IsRetransformData; }
            set
            {
                _Options.IsRetransformData = value;
                this.NotifyOfPropertyChange(() => this.IsRetransformData);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Flag to Mark bad below bottom.
        /// </summary>
        public bool IsMarkBadBelowBottom
        {
            get { return _Options.IsMarkBadBelowBottom; }
            set
            {
                _Options.IsMarkBadBelowBottom = value;
                this.NotifyOfPropertyChange(() => this.IsMarkBadBelowBottom);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Remove ship speed using the GPS data.
        /// </summary>
        public bool IsRemoveShipSpeedGPS
        {
            get { return _Options.IsRemoveShipSpeedGPS; }
            set
            {
                _Options.IsRemoveShipSpeedGPS = value;
                this.NotifyOfPropertyChange(() => this.IsRemoveShipSpeedGPS);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Remove ship speed using the Bottom Track data.
        /// </summary>
        public bool IsRemoveShipSpeedBT
        {
            get { return _Options.IsRemoveShipSpeedBT; }
            set
            {
                _Options.IsRemoveShipSpeedBT = value;
                this.NotifyOfPropertyChange(() => this.IsRemoveShipSpeedBT);

                // Save Options
                SaveOptions();
            }
        }


        #endregion

        #region Offsets

        /// <summary>
        /// Heading Offset
        /// </summary>
        public float HeadingOffset
        {
            get { return _Options.HeadingOffset; }
            set
            {
                _Options.HeadingOffset = value;
                this.NotifyOfPropertyChange(() => this.HeadingOffset);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Pitch Offset
        /// </summary>
        public float PitchOffset
        {
            get { return _Options.PitchOffset; }
            set
            {
                _Options.PitchOffset = value;
                this.NotifyOfPropertyChange(() => this.PitchOffset);

                // Save Options
                SaveOptions();
            }
        }

        /// <summary>
        /// Roll Offset
        /// </summary>
        public float RollOffset
        {
            get { return _Options.RollOffset; }
            set
            {
                _Options.RollOffset = value;
                this.NotifyOfPropertyChange(() => this.RollOffset);

                // Save Options
                SaveOptions();
            }
        }

        #endregion

        #region Good Bad Ens Count

        /// <summary>
        /// Number of good ensembles.
        /// </summary>
        private int _GoodEnsCount;
        /// <summary>
        /// Number of good ensembles.
        /// </summary>
        public int GoodEnsCount
        {
            get { return _GoodEnsCount; }
            set
            {
                _GoodEnsCount = value;
                this.NotifyOfPropertyChange(() => this.GoodEnsCount);
            }
        }

        /// <summary>
        /// Number of Bad Ensembles.
        /// </summary>
        private int _BadEnsCount;
        /// <summary>
        /// Number of Bad Ensembles.
        /// </summary>
        public int BadEnsCount
        {
            get { return _BadEnsCount; }
            set
            {
                _BadEnsCount = value;
                this.NotifyOfPropertyChange(() => this.BadEnsCount);
            }
        }

        /// <summary>
        /// Total number of Ensembles.
        /// </summary>
        private int _TotalEnsembles;
        /// <summary>
        /// Total Number of Ensembles.
        /// </summary>
        public int TotalEnsembles
        {
            get { return _TotalEnsembles; }
            set
            {
                _TotalEnsembles = value;
                this.NotifyOfPropertyChange(() => this.TotalEnsembles);
            }
        }

        /// <summary>
        /// Tooltip to describe the results.
        /// </summary>
        private string _GoodBadEnsTooltip;
        /// <summary>
        /// Tooltip to describe the results.
        /// </summary>
        public string GoodBadEnsTooltip
        {
            get { return _GoodBadEnsTooltip; }
            set
            {
                _GoodBadEnsTooltip = value;
                this.NotifyOfPropertyChange(() => this.GoodBadEnsTooltip);
            }
        }

        /// <summary>
        /// Folder path of output.
        /// </summary>
        private string _OutputFilePath;
        /// <summary>
        /// Folder path of output.
        /// </summary>
        public string OutputFilePath
        {
            get { return _OutputFilePath; }
            set
            {
                _OutputFilePath = value;
                this.NotifyOfPropertyChange(() => this.OutputFilePath);
            }
        }

        #endregion

        #endregion

        #region Commands

        /// <summary>
        /// Command to select a folder.
        /// </summary>
        public ReactiveCommand<object> SelectFolderCommand { get; protected set; }

        /// <summary>
        /// Command to import RTB data.
        /// </summary>
        public ReactiveCommand<object> ImportRtbDataCommand { get; protected set; }

        #endregion

        /// <summary>
        /// Initialize the view model.
        /// </summary>
        /// <param name="name">Name of the view.</param>
        public ExporterViewModel(string name)
        {
            base.DisplayName = name;

            _exportWriters = new Dictionary<SubsystemConfiguration, ExportWriter>();

            // Coordinate Transform list
            CoordinateTransformList = new List<string>();
            CoordinateTransformList.Add(XFORM_BEAM);
            CoordinateTransformList.Add(XFORM_INSTRUMENT);
            CoordinateTransformList.Add(XFORM_EARTH);

            _Options = new ExportOptions();
            CheckOptions();
            this.NotifyOfPropertyChange(null);
               
            ResultsFilePath = @"C:\RTI_Capture";
            IsExporting = false;

            IsCsvSelected = true;
            IsMatlabSelected = true;
            IsPd0Selected = true;
            IsEnsSelected = true;
            IsMatlabMatrixSelected = true;

            GoodEnsCount = 0;
            BadEnsCount = 0;
            TotalEnsembles = 0;

            // Dialog to import RTB data
            SelectFolderCommand = ReactiveCommand.Create();
            SelectFolderCommand.Subscribe(_ => SelectFolder());

            // Dialog to import RTB data
            ImportRtbDataCommand = ReactiveCommand.Create();
            ImportRtbDataCommand.Subscribe(_ => ImportRTB());
        }

        public void Dispose()
        {

        }

        /// <summary>
        /// Select a folder path.
        /// </summary>
        private void SelectFolder()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select a folder to store all the exported data.";
            var results = dialog.ShowDialog();

            if(results == DialogResult.OK)
            {
                // Clear counts
                GoodEnsCount = 0;
                BadEnsCount = 0;
                TotalEnsembles = 0;

                ResultsFilePath = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// Import the data.  Then export the data to selected formats.
        /// </summary>
        private async void ImportRTB()
        {
            try
            {
                // Show the FolderBrowserDialog.
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "Ensemble files (*.bin, *.ens)|*.bin; *.ens|BIN files (*.bin)|*.bin|ENS files (*.ens)|*.ens|DB files (*.db)|*.db|All files (*.*)|*.*";
                dialog.Multiselect = true;
                //dialog.InitialDirectory = Pulse.Commons.DEFAULT_RECORD_DIR;

                DialogResult result = dialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    // Clear previous results
                    GoodEnsCount = 0;
                    BadEnsCount = 0;
                    TotalEnsembles = 0;

                    // Get the files selected
                    string[] files = dialog.FileNames;

                    // Export data from the files
                    await ExportDataThread(files);
                }
            }
            catch (AccessViolationException ae)
            {
                log.Error("Error trying to open file", ae);
                IsExporting = false;
            }
            catch (Exception e)
            {
                log.Error("Error trying to open file", e);
                IsExporting = false;
            }
        }

        /// <summary>
        /// Run the export process in an background thread.  This will make the 
        /// screen not stall.
        /// </summary>
        /// <param name="files">Files to export.</param>
        /// <returns></returns>
        private async Task ExportDataThread(string[] files)
        {
            await Task.Run(() => ExportData(files));
        }

        /// <summary>
        /// Begin the export process.  Process each file.
        /// Export based off the selections and options.
        /// </summary>
        /// <param name="files">Files to read in and export data.</param>
        private void ExportData(string[] files)
        {
            IsExporting = true;

            // Clear the dictionary of previous use
            _exportWriters.Clear();

            foreach (var file in files)
            {
                // Create the folder for the exported data
                string folderpath = CreateProject(file);
                string filename = Path.GetFileNameWithoutExtension(file);

                // Get all the ensembles from the file
                FilePlayback fp = new FilePlayback();
                fp.GoodEnsembleEvent += Fp_GoodEnsembleEvent;
                fp.BadEnsembleEvent += Fp_BadEnsembleEvent;
                fp.FindRtbEnsembles(file);
                List<RTI.FilePlayback.EnsembleData> ensembles = fp.GetEnsembleDataList();
                fp.Dispose();

                if (ensembles.Count > 0)
                {
                    // Determine the correct options
                    // Because the number of ensembles and number of bins is not known for each file
                    SetProperOptions(ensembles);

                    try
                    {
                        // Write the data to exporter
                        WriteEnsembles(ensembles, folderpath, filename);
                    }
                    catch (AccessViolationException ae)
                    {
                        log.Error("Error trying to open file: " + file, ae);
                        IsExporting = false;
                    }
                    catch (Exception e)
                    {
                        log.Error("Error trying to open file: " + file, e);
                        IsExporting = false;
                    }
                }
            }

            // Close all the open exporters.
            foreach (var exporters in _exportWriters.Values)
            {
                exporters.Close(IsCsvSelected, IsMatlabSelected, IsMatlabMatrixSelected, IsPd0Selected, IsEnsSelected);
            }

            IsExporting = false;
        }

        /// <summary>
        /// Keep track of bad ensembles found.
        /// </summary>
        private void Fp_BadEnsembleEvent()
        {
            GoodEnsCount += 1;
            TotalEnsembles += 1;
            GoodBadEnsTooltip = "Good Ensembles: [" + GoodEnsCount + "] Bad Ensembles: [" + BadEnsCount  + "] Total Ensembles: [" + TotalEnsembles + "]";
        }

        /// <summary>
        /// Keep track of Good ensembles found.
        /// </summary>
        private void Fp_GoodEnsembleEvent()
        {
            BadEnsCount += 1;
            TotalEnsembles += 1;
            GoodBadEnsTooltip = "Good Ensembles: [" + GoodEnsCount + "] Bad Ensembles: [" + BadEnsCount + "] Total Ensembles: [" + TotalEnsembles + "]";
        }

        #region Write Ensembles

        /// <summary>
        /// Write all the ensembles to the exporters.  
        /// </summary>
        /// <param name="ensembles">Ensembles to write.</param>
        /// <param name="folderPath">Folder path.</param>
        /// <param name="filename">Filename without the extension.</param>
        private void WriteEnsembles(List<RTI.FilePlayback.EnsembleData> ensembles, string folderPath, string filename)
        {
            // Write all the ensembles to the exporters
            foreach(var ens in ensembles)
            {
                RTI.DataSet.Ensemble cloneEns = ens.Ensemble.Clone();

                lock (cloneEns.SyncRoot)
                {
                    // Screen the Ensemble based off the options
                    this.ScreenData(ref cloneEns, ens.OrigDataFormat);

                    // Write the data to the selected exporters
                    Write(cloneEns, folderPath, filename);
                }
            }
        }

        /// <summary>
        /// Write the ensemble to the Exporter writers.  Based off the selections, the different exporters will be written.
        /// </summary>
        /// <param name="ens">Ensemble to write.</param>
        /// <param name="folderPath">Folder path.</param>
        /// <param name="filename">File name.</param>
        private void Write(RTI.DataSet.Ensemble ens, string folderPath, string filename)
        {
            if (ens.IsEnsembleAvail)
            {
                if (!_exportWriters.ContainsKey(ens.EnsembleData.SubsystemConfig))
                {
                    // Add to the file name the cepo index and subsystem code
                    string filenameWithConfig = filename + string.Format("_{0}_{1}", ens.EnsembleData.SubsystemConfig.CommandSetupToString(), ens.EnsembleData.SubsystemConfig.SubSystem.DescString());

                    // Set the output path
                    OutputFilePath = folderPath + @"\" + filenameWithConfig;

                    // Create a writer for the subsystem configuration
                    ExportWriter ew = new ExportWriter();

                    // Clone the options for this object
                    // Each configuration needs its one options clone
                    // because the bin number or other settings can change
                    // between configurations
                    ExportOptions clonedOptions = _Options.Clone();
                    clonedOptions.UpdateOptions(ens);

                    ew.Open(folderPath, filenameWithConfig, clonedOptions, IsCsvSelected, IsMatlabSelected, IsMatlabMatrixSelected, IsPd0Selected, IsEnsSelected);

                    // Add the writer to the dictionary
                    _exportWriters.Add(ens.EnsembleData.SubsystemConfig, ew);
                }

                // Write the data
                _exportWriters[ens.EnsembleData.SubsystemConfig].Write(ens, IsCsvSelected, IsMatlabSelected, IsMatlabMatrixSelected, IsPd0Selected, IsEnsSelected);
            }
        }

        #endregion

        #region Create Project

        /// <summary>
        /// Create a folder to hold the new file.
        /// </summary>
        /// <param name="file">File path to file to export.</param>
        /// <returns>Folder path to store the results.</returns>
        private string CreateProject(string file)
        {
            // Create the directory to store the results
            if(!Directory.Exists(ResultsFilePath))
            {
                Directory.CreateDirectory(ResultsFilePath);
            }

            // Get the filname only
            string filename = Path.GetFileNameWithoutExtension(file);

            // Create a directory for the file name
            // Each file will get its own folder
            if(!Directory.Exists(ResultsFilePath + @"\" + filename))
            {
                Directory.CreateDirectory(ResultsFilePath + @"\" + filename + @"\");
            }

            return ResultsFilePath + @"\" + filename + @"\";
        }

        /// <summary>
        /// Add a backslash to the end of folder path if needed..
        /// </summary>
        /// <param name="path">Path to add the slash if needed.</param>
        /// <returns>Folder path with end back slash.</returns>
        private string PathAddBackslash(string path)
        {
            // They're always one character but EndsWith is shorter than
            // array style access to last path character. Change this
            // if performance are a (measured) issue.
            string separator1 = Path.DirectorySeparatorChar.ToString();
            string separator2 = Path.AltDirectorySeparatorChar.ToString();

            // Trailing white spaces are always ignored but folders may have
            // leading spaces. It's unusual but it may happen. If it's an issue
            // then just replace TrimEnd() with Trim(). Tnx Paul Groke to point this out.
            path = path.TrimEnd();

            // Argument is always a directory name then if there is one
            // of allowed separators then I have nothing to do.
            if (path.EndsWith(separator1) || path.EndsWith(separator2))
                return path;

            // If there is the "alt" separator then I add a trailing one.
            // Note that URI format (file://drive:\path\filename.ext) is
            // not supported in most .NET I/O functions then we don't support it
            // here too. If you have to then simply revert this check:
            // if (path.Contains(separator1))
            //     return path + separator1;
            //
            // return path + separator2;
            if (path.Contains(separator2))
                return path + separator2;

            // If there is not an "alt" separator I add a "normal" one.
            // It means path may be with normal one or it has not any separator
            // (for example if it's just a directory name). In this case I
            // default to normal as users expect.
            return path + separator1;
        }

        #endregion

        #region Options


        /// <summary>
        /// Update the properties based off the first ensemble.
        /// </summary>
        /// <param name="ensembles">List of ensembles.</param>
        private void SetProperOptions(List<RTI.FilePlayback.EnsembleData> ensembles)
        {
            // Set max number of ensembles if not set
            if(_Options.MaxEnsembleNumber == 0)
            {
                _Options.MaxEnsembleNumber = Convert.ToUInt32(ensembles.Count);
            }

            // Get the maxmimum number of bins
            int maxNumBins = RTI.DataSet.Ensemble.MAX_NUM_BINS;
            var ens = ensembles.First();
            if (ens.Ensemble.IsEnsembleAvail)
            {
                maxNumBins = ens.Ensemble.EnsembleData.NumBins - 1;
                MaximumBin = ens.Ensemble.EnsembleData.NumBins - 1;
            }

            //// Ensemble Numbers
            //MinEnsembleNumberEntry = 0;
            //if (_pm.IsProjectSelected)
            //{
            //    MaxEnsembleNumberEntry = (uint)_numEns - 1;
            //}
            //else
            //{
            //    MaxEnsembleNumberEntry = 0;
            //}


            // Set max number of bins if not set
            if(_Options.AmplitudeMaxBin == 0)
            {
                
                _Options.AmplitudeMaxBin = maxNumBins;
            }

            if(_Options.BeamMaxBin == 0)
            {
                
                _Options.BeamMaxBin = maxNumBins;
            }

            if(_Options.CorrelationMaxBin == 0)
            {
                
                _Options.CorrelationMaxBin = maxNumBins;
            }

            if(_Options.EarthMaxBin == 0)
            {
                
                _Options.EarthMaxBin = maxNumBins;
            }

            if (_Options.ShipMaxBin == 0)
            {

                _Options.ShipMaxBin = maxNumBins;
            }

            if (_Options.GoodBeamMaxBin == 0)
            {
                
                _Options.GoodBeamMaxBin = maxNumBins;
            }

            if(_Options.GoodEarthMaxBin == 0)
            {
                
                _Options.GoodEarthMaxBin = maxNumBins;
            }

            if(_Options.InstrumentMaxBin == 0)
            {
                
                _Options.InstrumentMaxBin = maxNumBins;
            }

            if(_Options.VelVectorMaxBin == 0)
            {
                
                _Options.VelVectorMaxBin = maxNumBins;
            }
            
            this.NotifyOfPropertyChange(null);
        }

        /// <summary>
        /// Save the options.
        /// </summary>
        private void SaveOptions()
        {

        }

        /// <summary>
        /// Check if any options have been set.  This
        /// will set all the options to the project settings.
        /// </summary>
        private void CheckOptions()
        {
            // Check if the options were set for Max Ensemble
            if (MaxEnsembleNumber <= 0)
            {
                MaxEnsembleNumber = MaxEnsembleNumberEntry;
            }

            if (AmplitudeMaxBin <= 0)
            {
                AmplitudeMaxBin = MaximumBin;
            }

            if (BeamMaxBin <= 0)
            {
                BeamMaxBin = MaximumBin;
            }

            if (InstrumentMaxBin <= 0)
            {
                InstrumentMaxBin = MaximumBin;
            }

            if (EarthMaxBin <= 0)
            {
                EarthMaxBin = MaximumBin;
            }

            if (ShipMaxBin <= 0)
            {
                EarthMaxBin = MaximumBin;
            }

            if (VelVectorMaxBin <= 0)
            {
                VelVectorMaxBin = MaximumBin;
            }

            if (CorrelationMaxBin <= 0)
            {
                CorrelationMaxBin = MaximumBin;
            }

            if (GoodBeamMaxBin <= 0)
            {
                GoodBeamMaxBin = MaximumBin;
            }

            if (GoodEarthMaxBin <= 0)
            {
                GoodEarthMaxBin = MaximumBin;
            }

            // Coordinate Transform
            switch (_Options.CoordinateTransform)
            {
                case PD0.CoordinateTransforms.Coord_Beam:
                    CoordinateTransform = XFORM_BEAM;
                    break;
                case PD0.CoordinateTransforms.Coord_Instrument:
                    CoordinateTransform = XFORM_INSTRUMENT;
                    break;
                case PD0.CoordinateTransforms.Coord_Earth:
                    CoordinateTransform = XFORM_EARTH;
                    break;
                default:
                    CoordinateTransform = XFORM_EARTH;
                    break;
            }
        }

        #endregion

        #region Screen data

        /// <summary>
        /// Screen the data.  This will apply all the screen options selected.
        /// </summary>
        /// <param name="ensemble">Ensemble to screen.</param>
        /// <param name="origDataFormat">This should always be the original format.</param>
        private void ScreenData(ref RTI.DataSet.Ensemble ensemble, RTI.AdcpCodec.CodecEnum origDataFormat)
        {
            // Set Pitch Offset 
            // Pitch Offset must go before Heading offset or Retransform
            if (PitchOffset != 0.0f || RollOffset != 0.0f)
            {
                RTI.VesselMount.VmTiltOffset.AddAncillaryTiltOffset(ref ensemble, PitchOffset, RollOffset);
                RTI.VesselMount.VmTiltOffset.AddBottomTrackTiltOffset(ref ensemble, PitchOffset, RollOffset);

                // Retransform will be done below
            }

            // Set Heading Offset
            if (HeadingOffset != 0.0f)
            {
                RTI.VesselMount.VmHeadingOffset.AddAncillaryHeadingOffset(ref ensemble, HeadingOffset);
                RTI.VesselMount.VmHeadingOffset.AddBottomTrackHeadingOffset(ref ensemble, HeadingOffset);

                // Retransform will be done below
            }

            // Retransform the data
            // If the Heading, Pitch or Roll offset is used, then the data must be retransformed
            if (IsRetransformData || HeadingOffset != 0.0f || PitchOffset != 0.0f || RollOffset != 0.0f)
            {
                // PD0 has a different cooridiate matrix
                // And the beams are in different positions
                Transform.ProfileTransform(ref ensemble, origDataFormat);
                Transform.BottomTrackTransform(ref ensemble, origDataFormat);

                // WaterMass transform data
                // This will also create the ship data
                if (ensemble.IsInstrumentWaterMassAvail)
                {
                    Transform.WaterMassTransform(ref ensemble, origDataFormat);
                }
            }

            // Mark Bad Below Bottom
            if (IsMarkBadBelowBottom)
            {
                RTI.ScreenData.ScreenMarkBadBelowBottom.Screen(ref ensemble);
            }

            // Remove Ship Speed
            if (IsRemoveShipSpeedBT | IsRemoveShipSpeedGPS)
            {
                RTI.ScreenData.RemoveShipSpeed.RemoveVelocity(ref ensemble, _prevBtEast, _prevBtNorth, _prevBtVert, IsRemoveShipSpeedBT, IsRemoveShipSpeedGPS);
                RTI.ScreenData.RemoveShipSpeed.RemoveVelocityInstrument(ref ensemble, _prevShipSpeedX, _prevShipSpeedY, _prevShipSpeedZ, IsRemoveShipSpeedBT, IsRemoveShipSpeedGPS);
                RTI.ScreenData.RemoveShipSpeed.RemoveVelocityShip(ref ensemble, _prevShipSpeedTransverse, _prevShipSpeedLongitudinal, _prevShipSpeedNormal, IsRemoveShipSpeedBT, IsRemoveShipSpeedGPS);

                // Create the new velocity vectors based off the new data
                RTI.DataSet.VelocityVectorHelper.CreateVelocityVector(ref ensemble);
            }

            // Record the previous ship speed values
            SetPreviousShipSpeed(ensemble);
        }

        #endregion

        #region Previous Ship Speed

        /// <summary>
        /// Store the previous Ship speed for the different velocity coordinate transforms.
        /// </summary>
        /// <param name="ens">Ensembles.</param>
        private void SetPreviousShipSpeed(RTI.DataSet.Ensemble ens)
        {
            // EARTH
            // Record the Bottom for previous values
            float[] prevShipSpeed = RTI.ScreenData.RemoveShipSpeed.GetPreviousShipSpeed(ens, 0, IsRemoveShipSpeedBT, IsRemoveShipSpeedGPS);
            _prevBtEast = prevShipSpeed[0];
            _prevBtNorth = prevShipSpeed[1];
            _prevBtVert = prevShipSpeed[2];

            // Instrument
            // Record the Bottom for previous values
            float[] prevShipSpeedInstrument = RTI.ScreenData.RemoveShipSpeed.GetPreviousShipSpeedInstrument(ens, 0, IsRemoveShipSpeedBT, IsRemoveShipSpeedGPS);
            _prevShipSpeedX = prevShipSpeedInstrument[0];
            _prevShipSpeedY = prevShipSpeedInstrument[1];
            _prevShipSpeedZ = prevShipSpeedInstrument[2];

            // Ship
            // Record the Bottom for previous values
            float[] prevShipSpeedShip = RTI.ScreenData.RemoveShipSpeed.GetPreviousShipSpeedShip(ens, 0, IsRemoveShipSpeedBT, IsRemoveShipSpeedGPS);
            _prevShipSpeedTransverse = prevShipSpeedShip[0];
            _prevShipSpeedLongitudinal = prevShipSpeedShip[1];
            _prevShipSpeedNormal = prevShipSpeedShip[2];
        }

        #endregion


    }
}

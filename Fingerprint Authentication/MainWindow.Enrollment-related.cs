﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DPFP.Processing;
using System.Threading.Tasks;
using DPFP;

namespace Fingerprint_Authentication
{
	// Fingerprint enrollment methods are here
    partial class MainWindow
    {
        private Enrollment enroller;

        private void initialiseEnroller()
        {
            try
            {
                enroller = new Enrollment();
                WriteGoodStatus("Enroller initialised.");
            }
            catch
            {
                WriteErrorStatus("Could not initialise enroller.");
            }
        }

        private void startEnrolling()
        {
            initialiseEnroller();
            noOfScansLeft = enroller.FeaturesNeeded;
            WriteStatus("Put your finger on the scanner.");
            WriteStatus($"Scans left: {noOfScansLeft}.");
        }

        private void createFeatureAndAddItToTheEnroller(Sample sample)
        {
            FeatureSet feature = ExtractFeatures(sample, DataPurpose.Enrollment);

            if (feature != null)
            {
                WriteGoodStatus("The fingerprint feature set was created.");
                enroller.AddFeatures(feature);
                noOfScansLeft--;
            }

            if (noOfScansLeft != 0)
                WriteStatus($"Scans left: {noOfScansLeft}.");
        }

        private void processEnrollment(Sample sample)
        {
            createFeatureAndAddItToTheEnroller(sample);

            switch (enroller.TemplateStatus)
            {
                case Enrollment.Status.Ready:   // Report success and stop capturing
                    StopCapturing();
                    // Todo: Add call to save the enrolled fingerprint
                    break;

                case Enrollment.Status.Failed:  // Report failure and restart capturing
                    enroller.Clear();
                    StopCapturing();
                    StartCapturing();
                    break;
                    
            }
        }
    }
}
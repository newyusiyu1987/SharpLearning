﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpLearning.Neural.Layers;
using MathNet.Numerics.LinearAlgebra;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace SharpLearning.Neural.Test.Layers
{
    [TestClass]
    public class BatchNormalizationLayerTest
    {
        [TestMethod]
        public void BatchNormalizationLayer_CopyLayerForPredictionModel()
        {
            var batchSize = 1;
            var random = new Random(232);

            var sut = new BatchNormalizationLayer();
            sut.Initialize(3, 3, 1, batchSize, random);

            var layers = new List<ILayer>();
            sut.CopyLayerForPredictionModel(layers);

            var actual = (BatchNormalizationLayer)layers.Single();

            Assert.AreEqual(sut.Width, actual.Width);
            Assert.AreEqual(sut.Height, actual.Height);
            Assert.AreEqual(sut.Depth, actual.Depth);

            MatrixAsserts.AreEqual(sut.Scale, actual.Scale);
            MatrixAsserts.AreEqual(sut.Bias, actual.Bias);

            // batch means and vars are copied to actual means and vars and are used during test/model predictions.
            // Batch means and vars should be null during test/model predictions.
            MatrixAsserts.AreEqual(sut.BatchColumnMeans, actual.ColumnMeans);
            MatrixAsserts.AreEqual(sut.BatchcolumnVars, actual.ColumnVars);
            Assert.IsNull(actual.BatchColumnMeans);
            Assert.IsNull(actual.BatchcolumnVars);


            Assert.AreEqual(sut.OutputActivations.RowCount, actual.OutputActivations.RowCount);
            Assert.AreEqual(sut.OutputActivations.ColumnCount, actual.OutputActivations.ColumnCount);
        }

        [TestMethod]
        public void BatchNormalizationLayer_Forward()
        {
            const int fanIn = 4;
            const int batchSize = 2;
            var random = new Random(232);

            var sut = new BatchNormalizationLayer();
            sut.Initialize(1, 1, fanIn, batchSize, random);

            var data = new float[] { 0, 1, -1, 1, 0.5f, 1.5f, -10, 10 };                        
            var input = Matrix<float>.Build.Dense(batchSize, fanIn, data);

            Trace.WriteLine(input.ToString());

            var actual = sut.Forward(input);

            Trace.WriteLine(string.Join(", ", actual.ToColumnMajorArray()));
            Trace.WriteLine(actual);

            var expected = Matrix<float>.Build.Dense(batchSize, fanIn, new float[] { -0.09664511f, 0.09664511f, -0.6003481f, 0.6003481f, 0.1156925f, -0.1156925f, -0.2164436f, 0.2164436f });
            MatrixAsserts.AreEqual(expected, actual);
        }

        [TestMethod]
        public void BatchNormalizationLayer_Forward_SpatialInput()
        {
            var batchSize = 2;

            var filterHeight = 2;
            var filterWidth = 2;
            var filterDepth = 2;

            var stride = 1;
            var padding = 0;

            var inputWidth = 3;
            var inputHeight = 3;
            var inputDepth = 3;

            var filterGridWidth = ConvUtils.GetFilterGridLength(inputWidth, filterWidth, stride, padding);
            var filterGridHeight = ConvUtils.GetFilterGridLength(inputHeight, filterHeight, stride, padding);

            var k = filterDepth;

            var input = new float[] { 111, 121, 112, 122, 113, 123, 114, 124, 211, 221, 212, 222, 213, 223, 214, 224 };
            var convInput = Matrix<float>.Build.Dense(2, 8, input);
            var rowWiseInput = Matrix<float>.Build.Dense(batchSize, k * filterGridWidth * filterGridHeight);

            Trace.WriteLine(convInput);

            ConvUtils.ReshapeConvolutionsToRowMajor(convInput, inputDepth, inputHeight, inputWidth, filterHeight, filterWidth,
                padding, padding, stride, stride, rowWiseInput);

            Trace.WriteLine(rowWiseInput);

            var random = new Random(232);

            var sut = new BatchNormalizationLayer();
            sut.Initialize(filterGridWidth, filterGridHeight, filterDepth, batchSize, random);

            var actual = sut.Forward(rowWiseInput);

            Trace.WriteLine(string.Join(", ", actual.ToColumnMajorArray()));
            Trace.WriteLine(actual);

            var expected = Matrix<float>.Build.Dense(batchSize, k * filterGridWidth * filterGridHeight, new float[] { -0.07037111f, 0.06627183f, -0.06900468f, 0.06763826f, -0.06763826f, 0.06900468f, -0.06627183f, 0.07037111f, -0.4371365f, 0.4116722f, -0.4286484f, 0.4201603f, -0.4201603f, 0.4286484f, -0.4116722f, 0.4371365f });
            MatrixAsserts.AreEqual(expected, actual);
        }

        [TestMethod]
        public void BatchNormalizationLayer_Backward()
        {
            const int fanIn = 4;
            const int batchSize = 2;
            var random = new Random(232);

            var sut = new BatchNormalizationLayer();
            sut.Initialize(1, 1, fanIn, batchSize, random);

            var data = new float[] { 0, 1, -1, 1, 0.5f, 1.5f, -10, 10 };
            var input = Matrix<float>.Build.Dense(batchSize, fanIn, data);

            Trace.WriteLine(input.ToString());
            sut.Forward(input);

            var delta = Matrix<float>.Build.Random(batchSize, fanIn, random.Next());
            var actual = sut.Backward(delta);

            Trace.WriteLine(string.Join(", ", actual.ToColumnMajorArray()));

            var expected = Matrix<float>.Build.Dense(batchSize, fanIn, new float[] { -2.600517E-06f, 2.615418E-06f, -1.349278E-06f, 1.349278E-06f, 1.158319E-06f, -1.150868E-06f, -5.639333E-10f, -9.261829E-10f });
            MatrixAsserts.AreEqual(expected, actual);
        }

        [TestMethod]
        public void BatchNormalizationLayer_GradientCheck_BatchSize_1()
        {
            const int fanIn = 5;
            const int batchSize = 1;

            var sut = new BatchNormalizationLayer();
            GradientCheckTools.CheckLayer(sut, 1, 1, fanIn, batchSize, 1e-4f, new Random(21));
        }

        [TestMethod]
        public void BatchNormalizationLayer_GradientCheck_BatchSize_10()
        {
            const int fanIn = 5;
            const int batchSize = 10;

            var sut = new BatchNormalizationLayer();
            GradientCheckTools.CheckLayer(sut, 1, 1, fanIn, batchSize, 1e-4f, new Random(21));
        }
    }
}

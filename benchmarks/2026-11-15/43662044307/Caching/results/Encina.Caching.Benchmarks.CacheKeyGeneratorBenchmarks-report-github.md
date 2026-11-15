```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 3           | 2,443.2 ns |  8.05 ns |  4.79 ns | 2,442.8 ns |  0.66 |    0.00 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     | 3           |   148.5 ns |  1.53 ns |  1.01 ns |   148.2 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     | 3           |   328.1 ns |  2.85 ns |  1.89 ns |   327.4 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3           | 4,647.4 ns | 40.75 ns | 26.95 ns | 4,640.0 ns |  1.26 |    0.01 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3           | 3,676.9 ns | 18.88 ns | 12.49 ns | 3,671.6 ns |  1.00 |    0.00 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |             |            |          |          |            |       |         |        |           |             |
| GenerateKey_WithTemplate     | MediumRun  | 15             | 2           | 10          | 2,458.7 ns | 28.24 ns | 39.58 ns | 2,427.8 ns |  0.67 |    0.02 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | MediumRun  | 15             | 2           | 10          |   146.0 ns |  0.49 ns |  0.74 ns |   146.2 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | MediumRun  | 15             | 2           | 10          |   328.3 ns |  1.66 ns |  2.44 ns |   328.4 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | MediumRun  | 15             | 2           | 10          | 4,885.9 ns | 58.80 ns | 88.01 ns | 4,904.0 ns |  1.34 |    0.04 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | MediumRun  | 15             | 2           | 10          | 3,649.6 ns | 51.57 ns | 73.96 ns | 3,592.8 ns |  1.00 |    0.03 | 0.0572 |    1000 B |        1.00 |

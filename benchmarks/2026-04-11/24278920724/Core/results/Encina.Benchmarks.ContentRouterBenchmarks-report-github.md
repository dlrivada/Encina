```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 3           | 1,735.8 ns | 10.67 ns |  7.06 ns | 1,734.4 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 3           | 1,109.1 ns |  6.78 ns |  3.54 ns | 1,107.8 ns |  0.64 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 3           | 1,087.2 ns |  5.83 ns |  3.47 ns | 1,086.3 ns |  0.63 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 3           | 1,805.9 ns |  8.36 ns |  4.97 ns | 1,805.1 ns |  1.04 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 3           | 1,765.6 ns | 13.87 ns |  9.17 ns | 1,763.6 ns |  1.02 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | 3           |   430.1 ns | 10.29 ns |  6.80 ns |   430.7 ns |  0.25 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | 3           |   700.7 ns |  6.96 ns |  4.14 ns |   700.8 ns |  0.40 | 0.0525 |    1320 B |        1.34 |
|                              |            |                |             |             |            |          |          |            |       |        |           |             |
| SimpleRoute_SingleCondition  | MediumRun  | 15             | 2           | 10          | 1,700.4 ns |  6.87 ns |  9.63 ns | 1,701.8 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | MediumRun  | 15             | 2           | 10          | 1,087.9 ns |  1.02 ns |  1.49 ns | 1,088.2 ns |  0.64 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | MediumRun  | 15             | 2           | 10          | 1,080.3 ns |  2.21 ns |  3.17 ns | 1,081.5 ns |  0.64 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | MediumRun  | 15             | 2           | 10          | 1,782.3 ns | 11.96 ns | 17.16 ns | 1,787.9 ns |  1.05 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | MediumRun  | 15             | 2           | 10          | 1,791.0 ns |  8.59 ns | 12.05 ns | 1,799.6 ns |  1.05 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | MediumRun  | 15             | 2           | 10          |   433.3 ns |  6.16 ns |  9.21 ns |   431.0 ns |  0.25 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | MediumRun  | 15             | 2           | 10          |   725.1 ns |  3.56 ns |  5.10 ns |   724.3 ns |  0.43 | 0.0525 |    1320 B |        1.34 |

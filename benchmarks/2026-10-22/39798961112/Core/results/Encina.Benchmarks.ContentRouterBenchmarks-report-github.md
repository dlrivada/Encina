```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 3           | 2,417.2 ns | 31.89 ns | 18.97 ns |  1.00 |    0.01 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 3           | 1,691.0 ns |  1.86 ns |  0.97 ns |  0.70 |    0.01 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 3           | 1,693.8 ns | 15.60 ns |  9.28 ns |  0.70 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 3           | 2,280.1 ns | 17.46 ns | 10.39 ns |  0.94 |    0.01 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 3           | 2,270.9 ns | 18.65 ns | 11.10 ns |  0.94 |    0.01 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | 3           |   416.0 ns |  3.61 ns |  2.15 ns |  0.17 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | 3           |   685.9 ns |  8.15 ns |  4.85 ns |  0.28 |    0.00 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |             |            |          |          |       |         |        |           |             |
| SimpleRoute_SingleCondition  | MediumRun  | 15             | 2           | 10          | 2,409.5 ns |  7.76 ns | 11.14 ns |  1.00 |    0.01 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | MediumRun  | 15             | 2           | 10          | 1,886.2 ns |  7.25 ns |  9.92 ns |  0.78 |    0.01 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | MediumRun  | 15             | 2           | 10          | 1,839.5 ns | 49.97 ns | 74.79 ns |  0.76 |    0.03 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | MediumRun  | 15             | 2           | 10          | 2,517.0 ns | 18.49 ns | 27.10 ns |  1.04 |    0.01 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | MediumRun  | 15             | 2           | 10          | 2,491.4 ns | 62.80 ns | 90.07 ns |  1.03 |    0.04 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | MediumRun  | 15             | 2           | 10          |   422.7 ns | 12.91 ns | 18.52 ns |  0.18 |    0.01 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | MediumRun  | 15             | 2           | 10          |   752.4 ns | 15.64 ns | 23.41 ns |  0.31 |    0.01 | 0.0782 |    1320 B |        1.34 |

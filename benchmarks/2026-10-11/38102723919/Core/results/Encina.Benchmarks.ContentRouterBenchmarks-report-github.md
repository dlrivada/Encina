```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 3           | 2,040.5 ns |  7.97 ns |  5.27 ns |  1.00 |    0.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 3           | 1,507.6 ns |  6.85 ns |  4.07 ns |  0.74 |    0.00 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 3           | 1,490.4 ns |  4.65 ns |  2.77 ns |  0.73 |    0.00 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 3           | 2,096.4 ns |  6.24 ns |  4.13 ns |  1.03 |    0.00 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 3           | 2,150.0 ns | 11.61 ns |  7.68 ns |  1.05 |    0.00 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | 3           |   380.4 ns |  1.58 ns |  1.04 ns |  0.19 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | 3           |   611.8 ns |  2.05 ns |  1.36 ns |  0.30 |    0.00 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |             |            |          |          |       |         |        |           |             |
| SimpleRoute_SingleCondition  | MediumRun  | 15             | 2           | 10          | 2,049.4 ns | 19.56 ns | 26.77 ns |  1.00 |    0.02 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | MediumRun  | 15             | 2           | 10          | 1,515.7 ns |  3.42 ns |  5.01 ns |  0.74 |    0.01 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | MediumRun  | 15             | 2           | 10          | 1,504.8 ns |  3.17 ns |  4.65 ns |  0.73 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | MediumRun  | 15             | 2           | 10          | 2,119.9 ns | 10.67 ns | 15.30 ns |  1.03 |    0.02 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | MediumRun  | 15             | 2           | 10          | 2,121.8 ns | 10.14 ns | 14.55 ns |  1.04 |    0.01 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | MediumRun  | 15             | 2           | 10          |   378.6 ns |  0.99 ns |  1.45 ns |  0.18 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | MediumRun  | 15             | 2           | 10          |   617.8 ns |  2.17 ns |  3.11 ns |  0.30 |    0.00 | 0.0782 |    1320 B |        1.34 |

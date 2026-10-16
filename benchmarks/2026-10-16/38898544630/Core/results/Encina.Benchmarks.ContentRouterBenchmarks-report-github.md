```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 2,033.5 ns |  23.16 ns | 13.78 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,483.7 ns |   3.83 ns |  2.53 ns |  0.73 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,480.8 ns |   2.65 ns |  1.58 ns |  0.73 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 2,115.5 ns |  13.68 ns |  8.14 ns |  1.04 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 2,117.4 ns |  15.43 ns | 10.20 ns |  1.04 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   367.8 ns |   1.89 ns |  1.25 ns |  0.18 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   630.7 ns |   8.18 ns |  5.41 ns |  0.31 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 2,098.6 ns |  84.85 ns |  4.65 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,516.8 ns |  78.87 ns |  4.32 ns |  0.72 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,526.3 ns | 132.62 ns |  7.27 ns |  0.73 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 2,139.1 ns | 256.07 ns | 14.04 ns |  1.02 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 2,126.4 ns | 148.96 ns |  8.17 ns |  1.01 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   389.0 ns |  19.27 ns |  1.06 ns |  0.19 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   648.4 ns |  51.91 ns |  2.85 ns |  0.31 | 0.0782 |    1320 B |        1.34 |

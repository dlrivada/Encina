```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 2,081.1 ns |  10.95 ns | 7.24 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,506.4 ns |   5.21 ns | 3.10 ns |  0.72 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,504.4 ns |   2.60 ns | 1.55 ns |  0.72 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 2,140.0 ns |   8.88 ns | 5.29 ns |  1.03 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 2,150.0 ns |   7.24 ns | 4.79 ns |  1.03 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   400.2 ns |   1.56 ns | 1.03 ns |  0.19 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   662.8 ns |   6.94 ns | 4.59 ns |  0.32 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |            |           |         |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 2,060.7 ns | 137.63 ns | 7.54 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,514.6 ns |  12.54 ns | 0.69 ns |  0.73 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,512.1 ns |  52.91 ns | 2.90 ns |  0.73 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 2,157.8 ns | 146.48 ns | 8.03 ns |  1.05 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 2,140.6 ns | 172.50 ns | 9.46 ns |  1.04 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   399.1 ns |  14.40 ns | 0.79 ns |  0.19 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   652.7 ns |  75.20 ns | 4.12 ns |  0.32 | 0.0782 |    1320 B |        1.34 |

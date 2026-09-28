```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,370.3 ns |  23.90 ns | 14.22 ns |  1.00 |    0.01 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     |   961.2 ns |  19.10 ns | 12.63 ns |  0.70 |    0.01 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     |   970.7 ns |  18.07 ns | 11.95 ns |  0.71 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,408.5 ns |  12.75 ns |  6.67 ns |  1.03 |    0.01 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,424.9 ns |  28.98 ns | 19.17 ns |  1.04 |    0.02 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   221.2 ns |   1.28 ns |  0.67 ns |  0.16 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   385.0 ns |   2.27 ns |  1.35 ns |  0.28 |    0.00 | 0.0787 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |         |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,329.7 ns |  53.90 ns |  2.95 ns |  1.00 |    0.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           |   969.0 ns |  45.00 ns |  2.47 ns |  0.73 |    0.00 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           |   956.4 ns | 181.77 ns |  9.96 ns |  0.72 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,409.0 ns | 136.00 ns |  7.45 ns |  1.06 |    0.01 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,382.9 ns | 129.58 ns |  7.10 ns |  1.04 |    0.01 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   213.9 ns |  20.83 ns |  1.14 ns |  0.16 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   388.0 ns | 120.14 ns |  6.59 ns |  0.29 |    0.00 | 0.0787 |    1320 B |        1.34 |

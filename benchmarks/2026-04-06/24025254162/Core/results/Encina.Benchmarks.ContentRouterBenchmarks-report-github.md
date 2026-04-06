```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,650.5 ns |   5.93 ns | 3.92 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,071.6 ns |   4.04 ns | 2.67 ns |  0.65 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,068.0 ns |   2.70 ns | 1.61 ns |  0.65 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,735.4 ns |   3.31 ns | 2.19 ns |  1.05 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,698.7 ns |   4.76 ns | 2.49 ns |  1.03 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   402.8 ns |   6.96 ns | 4.60 ns |  0.24 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   662.0 ns |   6.48 ns | 3.86 ns |  0.40 | 0.0525 |    1320 B |        1.34 |
|                              |            |                |             |            |           |         |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,712.0 ns |  92.85 ns | 5.09 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,075.5 ns |   5.93 ns | 0.33 ns |  0.63 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,062.9 ns |  60.29 ns | 3.30 ns |  0.62 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,833.8 ns |  25.30 ns | 1.39 ns |  1.07 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,789.9 ns |  18.67 ns | 1.02 ns |  1.05 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   408.8 ns |  44.36 ns | 2.43 ns |  0.24 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   712.5 ns | 171.05 ns | 9.38 ns |  0.42 | 0.0525 |    1320 B |        1.34 |

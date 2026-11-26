```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|------------:|----------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,309.4 ns |    34.10 ns |  22.56 ns |  1.00 |    0.02 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     |   936.6 ns |    21.59 ns |  12.85 ns |  0.72 |    0.02 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     |   955.6 ns |    14.41 ns |   8.58 ns |  0.73 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,353.1 ns |    15.48 ns |   9.21 ns |  1.03 |    0.02 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,416.1 ns |    30.91 ns |  18.40 ns |  1.08 |    0.02 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   215.1 ns |     5.21 ns |   3.45 ns |  0.16 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   364.3 ns |    16.32 ns |  10.80 ns |  0.28 |    0.01 | 0.0787 |    1320 B |        1.34 |
|                              |            |                |             |            |             |           |       |         |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,370.8 ns |   302.95 ns |  16.61 ns |  1.00 |    0.01 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           |   982.4 ns |   352.22 ns |  19.31 ns |  0.72 |    0.01 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           |   970.6 ns |   191.60 ns |  10.50 ns |  0.71 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,410.5 ns | 2,064.94 ns | 113.19 ns |  1.03 |    0.07 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,383.7 ns |   356.30 ns |  19.53 ns |  1.01 |    0.02 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   209.2 ns |   244.58 ns |  13.41 ns |  0.15 |    0.01 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   368.0 ns |    87.52 ns |   4.80 ns |  0.27 |    0.00 | 0.0787 |    1320 B |        1.34 |

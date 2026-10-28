```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,361.4 ns |  17.75 ns | 10.56 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,041.6 ns |  10.17 ns |  6.05 ns |  0.77 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,036.9 ns |  20.33 ns | 13.45 ns |  0.76 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,453.8 ns |  29.76 ns | 17.71 ns |  1.07 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,464.9 ns |  20.82 ns | 13.77 ns |  1.08 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   216.6 ns |   6.78 ns |  4.48 ns |  0.16 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   359.8 ns |  10.32 ns |  6.14 ns |  0.26 | 0.0787 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,430.8 ns | 118.46 ns |  6.49 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,101.0 ns | 163.08 ns |  8.94 ns |  0.77 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,062.1 ns |  67.35 ns |  3.69 ns |  0.74 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,474.3 ns | 207.65 ns | 11.38 ns |  1.03 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,465.9 ns | 111.04 ns |  6.09 ns |  1.02 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   218.1 ns |  89.91 ns |  4.93 ns |  0.15 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   370.0 ns | 174.76 ns |  9.58 ns |  0.26 | 0.0787 |    1320 B |        1.34 |

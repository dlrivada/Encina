```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,658.0 ns |  2.98 ns | 1.97 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,172.7 ns |  4.75 ns | 2.83 ns |  0.71 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,164.7 ns |  4.79 ns | 3.17 ns |  0.70 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,830.4 ns |  4.39 ns | 2.91 ns |  1.10 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,824.0 ns |  3.61 ns | 2.15 ns |  1.10 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   493.2 ns |  1.21 ns | 0.80 ns |  0.30 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   707.5 ns |  5.76 ns | 3.81 ns |  0.43 | 0.0525 |    1320 B |        1.34 |
|                              |            |                |             |            |          |         |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,667.1 ns | 77.37 ns | 4.24 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,145.4 ns | 54.29 ns | 2.98 ns |  0.69 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,141.7 ns | 69.51 ns | 3.81 ns |  0.68 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,752.8 ns | 26.09 ns | 1.43 ns |  1.05 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,772.1 ns | 61.33 ns | 3.36 ns |  1.06 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   484.4 ns | 19.55 ns | 1.07 ns |  0.29 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   696.7 ns | 62.47 ns | 3.42 ns |  0.42 | 0.0525 |    1320 B |        1.34 |

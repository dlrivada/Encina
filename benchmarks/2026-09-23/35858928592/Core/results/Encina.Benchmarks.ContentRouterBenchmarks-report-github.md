```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|---------:|---------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,696.4 ns |  6.49 ns |  3.86 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,088.1 ns |  5.47 ns |  3.62 ns |  0.64 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,124.8 ns |  5.84 ns |  3.48 ns |  0.66 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,830.0 ns |  6.55 ns |  4.33 ns |  1.08 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,839.8 ns |  7.22 ns |  4.77 ns |  1.08 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   502.9 ns |  3.01 ns |  1.79 ns |  0.30 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   720.9 ns | 17.62 ns | 11.66 ns |  0.42 | 0.0525 |    1320 B |        1.34 |
|                              |            |                |             |            |          |          |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,693.5 ns | 69.33 ns |  3.80 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,094.1 ns |  8.15 ns |  0.45 ns |  0.65 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,082.2 ns | 48.51 ns |  2.66 ns |  0.64 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,798.1 ns | 31.01 ns |  1.70 ns |  1.06 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,758.9 ns | 62.14 ns |  3.41 ns |  1.04 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   496.5 ns |  5.00 ns |  0.27 ns |  0.29 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   727.8 ns | 69.61 ns |  3.82 ns |  0.43 | 0.0525 |    1320 B |        1.34 |

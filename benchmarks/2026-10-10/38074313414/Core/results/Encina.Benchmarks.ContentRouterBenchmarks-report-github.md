```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,642.5 ns |  26.94 ns | 17.82 ns |  1.00 |    0.01 | 0.0114 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,294.7 ns |  10.18 ns |  6.74 ns |  0.79 |    0.01 | 0.0057 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,313.8 ns |  12.03 ns |  7.16 ns |  0.80 |    0.01 | 0.0057 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,673.2 ns |  60.10 ns | 39.75 ns |  1.02 |    0.03 | 0.0114 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,709.3 ns |  31.18 ns | 20.63 ns |  1.04 |    0.02 | 0.0114 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   369.0 ns |   2.04 ns |  1.21 ns |  0.22 |    0.00 | 0.0067 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   628.6 ns |   4.96 ns |  2.95 ns |  0.38 |    0.00 | 0.0153 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |         |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,628.5 ns | 395.00 ns | 21.65 ns |  1.00 |    0.02 | 0.0114 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,280.4 ns | 153.07 ns |  8.39 ns |  0.79 |    0.01 | 0.0057 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,267.0 ns | 244.42 ns | 13.40 ns |  0.78 |    0.01 | 0.0057 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,644.0 ns |  74.42 ns |  4.08 ns |  1.01 |    0.01 | 0.0114 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,650.8 ns | 480.36 ns | 26.33 ns |  1.01 |    0.02 | 0.0114 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   366.7 ns |  37.18 ns |  2.04 ns |  0.23 |    0.00 | 0.0067 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   642.0 ns | 280.77 ns | 15.39 ns |  0.39 |    0.01 | 0.0153 |    1320 B |        1.34 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,766.2 ns |  16.75 ns | 11.08 ns |  1.00 | 0.0114 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,376.8 ns |  27.93 ns | 18.47 ns |  0.78 | 0.0057 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,375.9 ns |  22.31 ns | 14.76 ns |  0.78 | 0.0057 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,782.3 ns |   7.82 ns |  4.09 ns |  1.01 | 0.0114 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,790.6 ns |  19.10 ns | 12.63 ns |  1.01 | 0.0114 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   392.5 ns |   1.25 ns |  0.74 ns |  0.22 | 0.0067 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   665.9 ns |   3.55 ns |  2.35 ns |  0.38 | 0.0153 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,712.6 ns |  79.54 ns |  4.36 ns |  1.00 | 0.0114 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,377.0 ns | 304.46 ns | 16.69 ns |  0.80 | 0.0057 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,389.8 ns | 218.28 ns | 11.96 ns |  0.81 | 0.0057 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,846.2 ns | 103.76 ns |  5.69 ns |  1.08 | 0.0114 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,781.0 ns |  20.98 ns |  1.15 ns |  1.04 | 0.0114 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   388.0 ns |  18.35 ns |  1.01 ns |  0.23 | 0.0067 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   680.6 ns |  17.77 ns |  0.97 ns |  0.40 | 0.0153 |    1320 B |        1.34 |

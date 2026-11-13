```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,296.3 ns |   5.93 ns | 3.92 ns |  1.00 | 0.0114 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,052.1 ns |   2.87 ns | 1.71 ns |  0.81 | 0.0057 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,055.4 ns |   4.31 ns | 2.85 ns |  0.81 | 0.0057 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,376.8 ns |   6.02 ns | 3.98 ns |  1.06 | 0.0114 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,355.8 ns |  11.66 ns | 7.71 ns |  1.05 | 0.0114 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   303.3 ns |   0.94 ns | 0.56 ns |  0.23 | 0.0067 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   530.3 ns |   3.69 ns | 1.93 ns |  0.41 | 0.0153 |    1320 B |        1.34 |
|                              |            |                |             |            |           |         |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,307.8 ns |  37.78 ns | 2.07 ns |  1.00 | 0.0114 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,060.2 ns |  42.21 ns | 2.31 ns |  0.81 | 0.0057 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,056.7 ns |  37.87 ns | 2.08 ns |  0.81 | 0.0057 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,351.2 ns |  54.87 ns | 3.01 ns |  1.03 | 0.0114 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,356.3 ns | 180.40 ns | 9.89 ns |  1.04 | 0.0114 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   305.7 ns |  23.31 ns | 1.28 ns |  0.23 | 0.0067 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   527.6 ns |  55.20 ns | 3.03 ns |  0.40 | 0.0153 |    1320 B |        1.34 |

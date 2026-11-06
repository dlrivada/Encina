```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,450.2 ns |  27.14 ns | 16.15 ns |  1.00 |    0.01 | 0.0114 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,146.9 ns |   9.67 ns |  5.76 ns |  0.79 |    0.01 | 0.0057 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,127.0 ns |   2.94 ns |  1.95 ns |  0.78 |    0.01 | 0.0057 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,507.4 ns |  32.91 ns | 21.77 ns |  1.04 |    0.02 | 0.0114 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,517.5 ns |  26.44 ns | 17.49 ns |  1.05 |    0.02 | 0.0114 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   327.6 ns |   5.84 ns |  3.47 ns |  0.23 |    0.00 | 0.0067 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   551.1 ns |  13.92 ns |  9.21 ns |  0.38 |    0.01 | 0.0153 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |         |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,482.9 ns | 627.48 ns | 34.39 ns |  1.00 |    0.03 | 0.0114 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,151.7 ns | 192.17 ns | 10.53 ns |  0.78 |    0.02 | 0.0057 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,132.3 ns |  55.67 ns |  3.05 ns |  0.76 |    0.02 | 0.0057 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,496.1 ns | 102.71 ns |  5.63 ns |  1.01 |    0.02 | 0.0114 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,477.0 ns | 205.25 ns | 11.25 ns |  1.00 |    0.02 | 0.0114 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   326.3 ns |  32.69 ns |  1.79 ns |  0.22 |    0.00 | 0.0067 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   554.8 ns | 188.91 ns | 10.35 ns |  0.37 |    0.01 | 0.0153 |    1320 B |        1.34 |

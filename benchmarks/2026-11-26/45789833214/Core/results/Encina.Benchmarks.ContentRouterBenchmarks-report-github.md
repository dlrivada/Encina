```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,451.2 ns |  39.09 ns | 25.86 ns |  1.00 |    0.02 | 0.0114 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,142.1 ns |   9.30 ns |  6.15 ns |  0.79 |    0.01 | 0.0057 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,134.7 ns |  34.15 ns | 20.32 ns |  0.78 |    0.02 | 0.0057 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,517.1 ns |  39.88 ns | 26.38 ns |  1.05 |    0.02 | 0.0114 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,512.9 ns |  10.54 ns |  6.97 ns |  1.04 |    0.02 | 0.0114 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   338.0 ns |  10.85 ns |  7.18 ns |  0.23 |    0.01 | 0.0067 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   561.3 ns |  10.26 ns |  6.79 ns |  0.39 |    0.01 | 0.0153 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |         |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,448.9 ns | 136.17 ns |  7.46 ns |  1.00 |    0.01 | 0.0114 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,141.0 ns |  56.60 ns |  3.10 ns |  0.79 |    0.00 | 0.0057 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,142.9 ns | 133.88 ns |  7.34 ns |  0.79 |    0.01 | 0.0057 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,496.5 ns | 258.59 ns | 14.17 ns |  1.03 |    0.01 | 0.0114 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,461.7 ns |  20.60 ns |  1.13 ns |  1.01 |    0.00 | 0.0114 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   336.8 ns |  18.70 ns |  1.03 ns |  0.23 |    0.00 | 0.0067 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   585.6 ns | 367.05 ns | 20.12 ns |  0.40 |    0.01 | 0.0153 |    1320 B |        1.34 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,755.9 ns |  19.82 ns | 13.11 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,380.6 ns |   1.95 ns |  1.02 ns |  0.79 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,401.6 ns |  10.95 ns |  6.52 ns |  0.80 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,863.8 ns |   5.41 ns |  3.22 ns |  1.06 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,862.2 ns |   8.10 ns |  5.36 ns |  1.06 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   291.2 ns |   2.65 ns |  1.75 ns |  0.17 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   501.3 ns |  15.06 ns |  9.96 ns |  0.29 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,779.4 ns |  88.37 ns |  4.84 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,382.3 ns |  31.93 ns |  1.75 ns |  0.78 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,376.3 ns |  22.20 ns |  1.22 ns |  0.77 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,825.4 ns | 146.60 ns |  8.04 ns |  1.03 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,834.4 ns | 231.29 ns | 12.68 ns |  1.03 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   292.7 ns |   4.99 ns |  0.27 ns |  0.16 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   496.6 ns | 122.79 ns |  6.73 ns |  0.28 | 0.0782 |    1320 B |        1.34 |

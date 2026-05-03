```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 3           | 2,052.8 ns | 14.30 ns |  9.46 ns | 2,053.9 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 3           | 1,400.6 ns |  3.54 ns |  2.11 ns | 1,400.4 ns |  0.68 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 3           | 1,392.1 ns |  3.46 ns |  2.06 ns | 1,391.9 ns |  0.68 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 3           | 2,110.0 ns | 25.13 ns | 13.14 ns | 2,106.5 ns |  1.03 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 3           | 2,157.0 ns |  8.78 ns |  5.81 ns | 2,158.4 ns |  1.05 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | 3           |   374.2 ns |  2.76 ns |  1.44 ns |   374.2 ns |  0.18 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | 3           |   651.6 ns |  6.65 ns |  4.40 ns |   652.4 ns |  0.32 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |             |            |          |          |            |       |        |           |             |
| SimpleRoute_SingleCondition  | MediumRun  | 15             | 2           | 10          | 1,996.7 ns |  3.73 ns |  5.58 ns | 1,997.6 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | MediumRun  | 15             | 2           | 10          | 1,408.5 ns |  2.45 ns |  3.44 ns | 1,408.4 ns |  0.71 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | MediumRun  | 15             | 2           | 10          | 1,409.1 ns |  8.22 ns | 11.53 ns | 1,401.0 ns |  0.71 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | MediumRun  | 15             | 2           | 10          | 2,104.4 ns | 18.23 ns | 24.96 ns | 2,102.9 ns |  1.05 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | MediumRun  | 15             | 2           | 10          | 2,103.6 ns | 10.26 ns | 15.04 ns | 2,097.8 ns |  1.05 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | MediumRun  | 15             | 2           | 10          |   369.2 ns |  3.59 ns |  5.37 ns |   366.0 ns |  0.18 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | MediumRun  | 15             | 2           | 10          |   632.8 ns |  1.94 ns |  2.84 ns |   633.4 ns |  0.32 | 0.0782 |    1320 B |        1.34 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method            | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           | 753.8 ns | 10.84 ns |  6.45 ns |  1.10 |    0.01 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           | 689.3 ns |  5.67 ns |  3.75 ns |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 3           | 687.2 ns | 12.96 ns |  6.78 ns |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |          |          |          |       |         |        |           |             |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 751.5 ns | 13.54 ns | 19.42 ns |  1.09 |    0.03 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          | 671.9 ns | 10.17 ns | 13.92 ns |  0.98 |    0.03 | 0.0019 |      40 B |        1.00 |
| Generate          | MediumRun  | 15             | 2           | 10          | 688.9 ns |  8.18 ns | 11.46 ns |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |

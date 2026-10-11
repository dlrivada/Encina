```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method            | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           | 791.0 ns | 33.91 ns | 22.43 ns |  1.14 |    0.04 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           | 679.6 ns |  7.10 ns |  3.71 ns |  0.98 |    0.02 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 3           | 695.5 ns | 23.34 ns | 15.44 ns |  1.00 |    0.03 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |          |          |          |       |         |        |           |             |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 770.2 ns | 11.48 ns | 16.83 ns |  1.11 |    0.03 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          | 665.0 ns |  8.03 ns | 11.78 ns |  0.96 |    0.02 | 0.0019 |      40 B |        1.00 |
| Generate          | MediumRun  | 15             | 2           | 10          | 694.4 ns |  7.91 ns | 11.35 ns |  1.00 |    0.02 | 0.0019 |      40 B |        1.00 |

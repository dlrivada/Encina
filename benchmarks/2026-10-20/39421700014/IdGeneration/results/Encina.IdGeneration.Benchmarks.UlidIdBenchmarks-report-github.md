```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 991.7 ns |   2.19 ns | 1.45 ns |  1.08 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 891.5 ns |   5.29 ns | 3.50 ns |  0.97 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 914.5 ns |   3.04 ns | 1.81 ns |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 989.0 ns | 109.15 ns | 5.98 ns |  1.07 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 899.3 ns |  37.74 ns | 2.07 ns |  0.98 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 922.2 ns |  38.76 ns | 2.12 ns |  1.00 | 0.0019 |      40 B |        1.00 |

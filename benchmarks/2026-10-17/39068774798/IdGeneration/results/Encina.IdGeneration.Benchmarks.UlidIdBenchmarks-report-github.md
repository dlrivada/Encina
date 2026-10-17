```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,062.9 ns | 10.04 ns | 6.64 ns |  1.16 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   897.8 ns |  4.96 ns | 3.28 ns |  0.98 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   917.2 ns |  0.91 ns | 0.54 ns |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |            |          |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           |   995.3 ns | 38.17 ns | 2.09 ns |  0.99 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   901.7 ns | 84.16 ns | 4.61 ns |  0.89 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 1,007.9 ns | 83.26 ns | 4.56 ns |  1.00 | 0.0019 |      40 B |        1.00 |

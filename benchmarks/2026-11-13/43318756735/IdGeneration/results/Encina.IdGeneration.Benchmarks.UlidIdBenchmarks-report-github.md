```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 858.2 ns |  7.40 ns | 4.89 ns |  1.12 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 742.5 ns |  2.79 ns | 1.46 ns |  0.97 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 768.3 ns |  1.79 ns | 1.18 ns |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |          |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 885.0 ns | 93.09 ns | 5.10 ns |  1.14 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 741.6 ns | 74.28 ns | 4.07 ns |  0.96 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 776.2 ns | 30.91 ns | 1.69 ns |  1.00 | 0.0019 |      40 B |        1.00 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|----------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 865.3 ns |   6.48 ns | 4.29 ns |  1.10 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 761.2 ns |   3.29 ns | 1.96 ns |  0.97 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 785.4 ns |   3.95 ns | 2.62 ns |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |           |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 857.1 ns | 109.96 ns | 6.03 ns |  1.10 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 796.8 ns |   3.92 ns | 0.21 ns |  1.02 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 779.7 ns |  92.24 ns | 5.06 ns |  1.00 | 0.0019 |      40 B |        1.00 |

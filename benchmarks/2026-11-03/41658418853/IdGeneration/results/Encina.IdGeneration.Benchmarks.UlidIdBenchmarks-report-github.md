```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 861.8 ns |  4.27 ns | 2.54 ns |  1.11 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 798.3 ns |  2.46 ns | 1.62 ns |  1.03 | 0.0019 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 774.9 ns |  3.17 ns | 2.10 ns |  1.00 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |          |          |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 859.5 ns | 18.15 ns | 0.99 ns |  1.09 | 0.0067 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 790.1 ns | 93.31 ns | 5.11 ns |  1.00 | 0.0019 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 787.1 ns |  9.63 ns | 0.53 ns |  1.00 | 0.0019 |      40 B |        1.00 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 575.5 ns |  1.98 ns | 1.31 ns |  1.35 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 415.2 ns |  1.01 ns | 0.60 ns |  0.97 | 0.0005 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 426.9 ns |  1.99 ns | 1.32 ns |  1.00 | 0.0005 |      40 B |        1.00 |
|                   |            |                |             |          |          |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 589.5 ns | 20.09 ns | 1.10 ns |  1.39 | 0.0010 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           | 415.2 ns | 11.36 ns | 0.62 ns |  0.98 | 0.0005 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           | 424.6 ns | 11.90 ns | 0.65 ns |  1.00 | 0.0005 |      40 B |        1.00 |

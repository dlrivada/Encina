```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,050.6 ns |  2.78 ns | 1.84 ns |  1.07 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   951.1 ns |  4.60 ns | 2.74 ns |  0.97 |      - |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   980.6 ns |  3.29 ns | 2.18 ns |  1.00 |      - |      40 B |        1.00 |
|                   |            |                |             |            |          |         |       |        |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,044.7 ns | 82.19 ns | 4.50 ns |  1.09 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   969.7 ns | 61.78 ns | 3.39 ns |  1.01 |      - |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   955.9 ns | 31.61 ns | 1.73 ns |  1.00 |      - |      40 B |        1.00 |

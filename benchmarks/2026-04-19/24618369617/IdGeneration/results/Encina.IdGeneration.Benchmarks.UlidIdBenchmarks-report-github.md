```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4


```
| Method            | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |-----------:|--------:|--------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           | 1,013.0 ns | 2.65 ns | 1.58 ns |  1.06 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           |   917.2 ns | 2.03 ns | 1.06 ns |  0.96 | 0.0010 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 3           |   956.5 ns | 1.68 ns | 1.00 ns |  1.00 |      - |      40 B |        1.00 |
|                   |            |                |             |             |            |         |         |       |        |           |             |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 1,024.1 ns | 3.71 ns | 5.32 ns |  1.06 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          |   916.6 ns | 2.31 ns | 3.31 ns |  0.95 | 0.0010 |      40 B |        1.00 |
| Generate          | MediumRun  | 15             | 2           | 10          |   969.8 ns | 5.54 ns | 7.77 ns |  1.00 |      - |      40 B |        1.00 |

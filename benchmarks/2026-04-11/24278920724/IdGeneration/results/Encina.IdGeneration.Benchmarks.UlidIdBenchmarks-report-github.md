```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method            | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|-------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 3           | 1,102.6 ns | 14.59 ns |  9.65 ns | 1,104.4 ns |  1.14 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | 3           |   930.2 ns |  1.32 ns |  0.69 ns |   930.2 ns |  0.96 | 0.0010 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     | 3           |   964.1 ns |  2.39 ns |  1.25 ns |   964.0 ns |  1.00 |      - |      40 B |        1.00 |
|                   |            |                |             |             |            |          |          |            |       |        |           |             |
| Generate_ToString | MediumRun  | 15             | 2           | 10          | 1,014.3 ns | 10.27 ns | 14.40 ns | 1,025.8 ns |  1.06 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | MediumRun  | 15             | 2           | 10          |   925.4 ns |  6.09 ns |  8.53 ns |   931.9 ns |  0.96 |      - |      40 B |        1.00 |
| Generate          | MediumRun  | 15             | 2           | 10          |   959.0 ns |  1.09 ns |  1.49 ns |   958.9 ns |  1.00 |      - |      40 B |        1.00 |

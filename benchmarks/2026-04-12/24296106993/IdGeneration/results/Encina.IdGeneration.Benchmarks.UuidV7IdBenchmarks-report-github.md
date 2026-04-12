```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.38GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 967.9 ns | 3.97 ns | 2.36 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           | 393.8 ns | 1.74 ns | 1.04 ns |  0.41 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 969.2 ns | 1.04 ns | 0.69 ns |  1.00 |         - |          NA |
|                    |            |                |             |             |          |         |         |       |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 962.6 ns | 1.65 ns | 2.31 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          | 392.7 ns | 0.27 ns | 0.39 ns |  0.41 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 959.4 ns | 1.25 ns | 1.80 ns |  1.00 |         - |          NA |

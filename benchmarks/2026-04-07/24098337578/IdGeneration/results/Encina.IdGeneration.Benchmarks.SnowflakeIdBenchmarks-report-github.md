```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method               | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error   | StdDev  | Median   | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |------------ |---------:|--------:|--------:|---------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 3           | 241.6 ns | 0.12 ns | 0.08 ns | 241.6 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 3           | 241.6 ns | 0.17 ns | 0.10 ns | 241.6 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 3           | 241.6 ns | 0.12 ns | 0.08 ns | 241.7 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 3           | 239.6 ns | 0.14 ns | 0.10 ns | 239.6 ns |  0.99 |         - |          NA |
|                      |            |                |             |             |          |         |         |          |       |           |             |
| Generate_WithShardId | MediumRun  | 15             | 2           | 10          | 241.7 ns | 0.04 ns | 0.06 ns | 241.7 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | MediumRun  | 15             | 2           | 10          | 241.6 ns | 0.06 ns | 0.08 ns | 241.6 ns |  1.00 |         - |          NA |
| Generate             | MediumRun  | 15             | 2           | 10          | 241.6 ns | 0.04 ns | 0.07 ns | 241.6 ns |  1.00 |         - |          NA |
| ExtractShardId       | MediumRun  | 15             | 2           | 10          | 239.6 ns | 0.06 ns | 0.08 ns | 239.6 ns |  0.99 |         - |          NA |

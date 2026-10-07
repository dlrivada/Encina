```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.320 ns | 0.2867 ns | 0.0157 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 48.075 ns | 7.9564 ns | 0.4361 ns |  6.57 |    0.05 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 44.619 ns | 1.0116 ns | 0.0555 ns |  6.10 |    0.01 | 0.0091 |     152 B |        6.33 |

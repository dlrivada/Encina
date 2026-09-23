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
| CreatePosition |  7.659 ns |  1.739 ns | 0.0953 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 47.523 ns |  4.333 ns | 0.2375 ns |  6.21 |    0.07 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 44.004 ns | 14.418 ns | 0.7903 ns |  5.75 |    0.11 | 0.0091 |     152 B |        6.33 |

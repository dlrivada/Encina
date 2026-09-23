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
| CreatePosition |  7.616 ns |  3.759 ns | 0.2061 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 49.682 ns | 17.320 ns | 0.9494 ns |  6.53 |    0.19 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 45.387 ns |  3.962 ns | 0.2172 ns |  5.96 |    0.14 | 0.0091 |     152 B |        6.33 |

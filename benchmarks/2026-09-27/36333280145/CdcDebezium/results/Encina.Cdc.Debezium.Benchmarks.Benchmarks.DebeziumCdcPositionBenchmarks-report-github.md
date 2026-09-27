```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.710 ns |  3.372 ns | 0.1848 ns |  1.00 |    0.03 | 0.0010 |      24 B |        1.00 |
| FromBytes      | 56.019 ns | 14.713 ns | 0.8065 ns |  7.27 |    0.17 | 0.0120 |     304 B |       12.67 |
| ToBytes        | 46.558 ns | 12.864 ns | 0.7051 ns |  6.04 |    0.15 | 0.0060 |     152 B |        6.33 |

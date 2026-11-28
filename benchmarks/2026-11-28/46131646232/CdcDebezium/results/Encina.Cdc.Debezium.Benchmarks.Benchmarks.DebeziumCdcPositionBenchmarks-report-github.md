```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.751 ns |  1.109 ns | 0.0608 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 48.773 ns |  5.681 ns | 0.3114 ns |  6.29 |    0.06 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 37.762 ns | 10.030 ns | 0.5498 ns |  4.87 |    0.07 | 0.0018 |     152 B |        6.33 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.772 ns |  0.8766 ns | 0.0480 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 47.521 ns | 20.8611 ns | 1.1435 ns |  6.11 |    0.13 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 34.966 ns |  3.2082 ns | 0.1759 ns |  4.50 |    0.03 | 0.0018 |     152 B |        6.33 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  5.615 ns | 0.3277 ns | 0.0180 ns |  1.00 |    0.00 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 42.064 ns | 0.4828 ns | 0.0265 ns |  7.49 |    0.02 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 31.891 ns | 1.5965 ns | 0.0875 ns |  5.68 |    0.02 | 0.0018 |     152 B |        6.33 |

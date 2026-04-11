```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.57GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.178 ns | 0.0714 ns | 0.1069 ns |  1.00 |    0.02 | 0.0010 |      24 B |        1.00 |
| FromBytes      | 62.705 ns | 1.0708 ns | 1.6027 ns |  8.74 |    0.25 | 0.0120 |     304 B |       12.67 |
| ToBytes        | 48.813 ns | 0.5947 ns | 0.8717 ns |  6.80 |    0.16 | 0.0060 |     152 B |        6.33 |

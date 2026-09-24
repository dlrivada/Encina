```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.77GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.327 ns |  1.969 ns | 0.1079 ns |  1.00 |    0.02 | 0.0010 |      24 B |        1.00 |
| FromBytes      | 65.193 ns | 13.391 ns | 0.7340 ns |  8.90 |    0.14 | 0.0120 |     304 B |       12.67 |
| ToBytes        | 49.666 ns |  1.792 ns | 0.0982 ns |  6.78 |    0.09 | 0.0060 |     152 B |        6.33 |

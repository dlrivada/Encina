```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.58GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.695 ns | 0.0256 ns | 0.0375 ns |  1.00 |    0.01 | 0.0010 |      24 B |        1.00 |
| FromBytes      | 62.920 ns | 0.8282 ns | 1.2140 ns |  9.40 |    0.19 | 0.0120 |     304 B |       12.67 |
| ToBytes        | 46.308 ns | 0.3094 ns | 0.4631 ns |  6.92 |    0.08 | 0.0060 |     152 B |        6.33 |

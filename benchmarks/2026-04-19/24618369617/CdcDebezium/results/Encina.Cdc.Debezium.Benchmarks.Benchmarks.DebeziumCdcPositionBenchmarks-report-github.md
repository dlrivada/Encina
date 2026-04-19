```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.312 ns | 0.2452 ns | 0.3594 ns |  7.090 ns |  1.00 |    0.07 | 0.0010 |      24 B |        1.00 |
| FromBytes      | 56.831 ns | 0.8693 ns | 1.2467 ns | 56.502 ns |  7.79 |    0.41 | 0.0120 |     304 B |       12.67 |
| ToBytes        | 45.823 ns | 0.7622 ns | 1.1172 ns | 46.096 ns |  6.28 |    0.34 | 0.0060 |     152 B |        6.33 |

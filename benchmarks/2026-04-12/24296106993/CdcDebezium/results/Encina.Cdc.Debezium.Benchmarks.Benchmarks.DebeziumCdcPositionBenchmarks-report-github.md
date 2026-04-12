```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.554 ns | 0.0988 ns | 0.1448 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 55.400 ns | 0.9800 ns | 1.4365 ns |  7.34 |    0.23 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 44.470 ns | 0.4619 ns | 0.6771 ns |  5.89 |    0.14 | 0.0091 |     152 B |        6.33 |

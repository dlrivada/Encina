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
| CreatePosition |  7.046 ns | 0.0287 ns | 0.0402 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 50.063 ns | 0.2505 ns | 0.3672 ns |  7.11 |    0.06 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 41.698 ns | 0.1333 ns | 0.1912 ns |  5.92 |    0.04 | 0.0091 |     152 B |        6.33 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.244 ns | 0.3342 ns | 0.0183 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 55.566 ns | 8.8111 ns | 0.4830 ns |  6.74 |    0.05 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 45.445 ns | 5.5239 ns | 0.3028 ns |  5.51 |    0.03 | 0.0091 |     152 B |        6.33 |

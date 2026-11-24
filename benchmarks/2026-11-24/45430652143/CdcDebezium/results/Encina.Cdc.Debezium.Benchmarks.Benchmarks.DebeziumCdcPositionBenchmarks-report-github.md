```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.75GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.426 ns |  3.116 ns | 0.1708 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 51.257 ns | 17.836 ns | 0.9776 ns |  6.90 |    0.18 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 41.444 ns |  2.137 ns | 0.1171 ns |  5.58 |    0.11 | 0.0091 |     152 B |        6.33 |

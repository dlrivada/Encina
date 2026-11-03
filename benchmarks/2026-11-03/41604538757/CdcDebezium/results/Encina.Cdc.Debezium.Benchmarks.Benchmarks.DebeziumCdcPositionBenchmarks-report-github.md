```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.390 ns | 0.9668 ns | 0.0530 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 59.714 ns | 9.8303 ns | 0.5388 ns |  7.12 |    0.07 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 46.092 ns | 6.5118 ns | 0.3569 ns |  5.49 |    0.05 | 0.0091 |     152 B |        6.33 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.416 ns |  0.7415 ns | 0.0406 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 58.404 ns | 40.2097 ns | 2.2040 ns |  6.94 |    0.23 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 47.432 ns | 17.1134 ns | 0.9380 ns |  5.64 |    0.10 | 0.0091 |     152 B |        6.33 |

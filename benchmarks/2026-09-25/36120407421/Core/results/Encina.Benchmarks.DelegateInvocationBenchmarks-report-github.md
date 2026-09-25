```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     43.04 ns |    16.264 ns |   0.891 ns |     1.46 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.39 ns |     3.526 ns |   0.193 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,219.33 ns | 4,004.640 ns | 219.508 ns | 3,103.39 |   18.79 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    141.07 ns |    15.699 ns |   0.861 ns |     4.80 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     77.42 ns |     6.139 ns |   0.337 ns |     2.63 |    0.02 | 0.0105 |      - |     176 B |        1.57 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.64 ns |     5.598 ns |   0.307 ns |     1.06 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     35.35 ns |     4.452 ns |   0.244 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 65,466.13 ns | 8,955.677 ns | 490.891 ns | 1,851.99 |   16.32 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    135.92 ns |     5.563 ns |   0.305 ns |     3.85 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     88.28 ns |     9.574 ns |   0.525 ns |     2.50 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
